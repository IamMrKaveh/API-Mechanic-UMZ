using System.Runtime.CompilerServices;

namespace Tests.TestInfrastructure.ContainerEngine;

/// <summary>
/// Runs once when the test assembly is loaded (before any Testcontainers type is
/// used) and points Testcontainers at the best reachable container engine:
/// the Podman machine named pipe, then a Podman API service over TCP, then a
/// Docker-compatible endpoint as fallback. An explicit <c>DOCKER_HOST</c> is
/// never overwritten.
/// </summary>
/// <remarks>
/// The vstest test host runs at low integrity on Windows. Low integrity
/// processes can open the Podman pipe read-only, so the Testcontainers client
/// fails with "Access to the path is denied" even when Podman is healthy.
/// <c>Setup-PodmanTestcontainers.ps1</c> keeps a Podman API service listening on
/// TCP, which has no such restriction, so the TCP endpoints are probed before
/// the Docker fallback.
/// </remarks>
internal static class ContainerEngineBootstrap
{
    private const string DockerHostVariable = "DOCKER_HOST";
    private const string RyukDisabledVariable = "TESTCONTAINERS_RYUK_DISABLED";
    private const string WindowsDockerPipe = "npipe://./pipe/docker_engine";
    private const string UnixDockerSocket = "unix:///var/run/docker.sock";

    private static readonly string[] PodmanServiceEndpoints =
    {
        "tcp://localhost:2375",
        "tcp://127.0.0.1:2375",
    };

    private static ContainerEngineDescriptor? _resolved;
    private static string? _explicitHost;
    private static bool _reachable;

    /// <summary>The engine selected by detection, or <c>null</c> for the Testcontainers default.</summary>
    internal static ContainerEngineDescriptor? Resolved => _resolved;

    /// <summary>Whether the selected endpoint answered a reachability probe.</summary>
    internal static bool IsReachable => _reachable;

    /// <summary>Human-readable status used in skip messages and test logs.</summary>
    internal static string DescribeConfiguration()
    {
        if (_explicitHost is not null)
        {
            var state = _reachable ? "reachable" : "unreachable from this test process";
            return $"Container engine: explicit {DockerHostVariable}={_explicitHost} ({state}).";
        }

        if (_resolved is null)
            return "Container engine: auto-detection found no running Podman (Docker/default endpoint in use).";

        var reachability = _reachable ? string.Empty : " [unreachable from this test process]";
        return $"Container engine: {_resolved.Description}{reachability}.";
    }

    [ModuleInitializer]
    internal static void Initialize()
    {
        try
        {
            var isWindows = OperatingSystem.IsWindows();
            var dockerHost = Environment.GetEnvironmentVariable(DockerHostVariable);

            if (!string.IsNullOrWhiteSpace(dockerHost))
            {
                _explicitHost = dockerHost;
                _reachable = ContainerEngineEndpointProbe.IsReachable(dockerHost);
                Console.Error.WriteLine($"[tests] container engine: {DescribeConfiguration()}");
                return;
            }

            var preference = Environment.GetEnvironmentVariable(ContainerEngineResolver.EngineVariable);

            var podmanEnvironment = ProbePodman(isWindows, dockerHost, preference);
            var candidates = ContainerEngineResolver.ResolveCandidates(
                preference, dockerHost, isWindows, podmanEnvironment);

            ContainerEngineDescriptor? descriptor = null;

            foreach (var candidate in candidates)
            {
                if (!ContainerEngineEndpointProbe.IsReachable(candidate.DockerHost))
                    continue;

                descriptor = candidate;
                _reachable = true;
                break;
            }

            // A running Podman machine may be unreachable through named pipes for
            // this process; the TCP API service of the same machine still works.
            if (descriptor is null)
            {
                descriptor = ResolvePodmanService();

                if (descriptor is not null)
                    _reachable = true;
            }

            var allowDockerFallback = !string.Equals(preference, "podman", StringComparison.OrdinalIgnoreCase);
            if (descriptor is null && allowDockerFallback)
            {
                var dockerEndpoint = isWindows ? WindowsDockerPipe : UnixDockerSocket;
                if (ContainerEngineEndpointProbe.IsReachable(dockerEndpoint))
                {
                    descriptor = new ContainerEngineDescriptor(
                        ContainerEngineKind.Docker,
                        dockerEndpoint,
                        IsRootless: false,
                        DisableRyuk: false,
                        $"Docker-compatible engine at {dockerEndpoint}");
                    _reachable = true;
                }
            }

            // Keep the primary Podman endpoint for the message when nothing answered.
            descriptor ??= candidates.FirstOrDefault();

            if (descriptor is null)
                return;

            _resolved = descriptor;

            if (!_reachable)
            {
                Console.Error.WriteLine($"[tests] container engine: {DescribeConfiguration()}");
                return;
            }

            // Only a probed endpoint is pinned: an unreachable DOCKER_HOST makes
            // every container call fail with the endpoint name instead of letting
            // Testcontainers pick its own default.
            Environment.SetEnvironmentVariable(DockerHostVariable, descriptor.DockerHost);

            if (descriptor.DisableRyuk
                && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(RyukDisabledVariable)))
            {
                Environment.SetEnvironmentVariable(RyukDisabledVariable, "true");
            }

            Console.Error.WriteLine($"[tests] container engine: {DescribeConfiguration()}");
        }
        catch
        {
            // Engine detection must never break test discovery or execution.
        }
    }

    private static ContainerEngineDescriptor? ResolvePodmanService()
    {
        foreach (var endpoint in PodmanServiceEndpoints)
        {
            if (!ContainerEngineEndpointProbe.IsReachable(endpoint))
                continue;

            return new ContainerEngineDescriptor(
                ContainerEngineKind.Podman,
                endpoint,
                IsRootless: true,
                DisableRyuk: true,
                $"Podman machine API over TCP (podman system service) at {endpoint}");
        }

        return null;
    }

    private static PodmanEnvironment? ProbePodman(bool isWindows, string? dockerHost, string? preference)
    {
        if (!string.IsNullOrWhiteSpace(dockerHost)
            || string.Equals(preference, "docker", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var podmanPath = PodmanProbe.FindPodman(
            Environment.GetEnvironmentVariable(ContainerEngineResolver.PodmanPathVariable),
            isWindows,
            File.Exists,
            SplitPath(Environment.GetEnvironmentVariable("PATH")),
            PodmanProbe.KnownLocations(isWindows));

        if (podmanPath is null)
            return null;

        var machine = PodmanProbe.ReadMachine(PodmanProbe.Run, podmanPath);
        var host = PodmanProbe.ReadHost(PodmanProbe.Run, podmanPath);
        var connection = machine is null
            ? PodmanProbe.ReadDefaultConnection(PodmanProbe.Run, podmanPath)
            : null;

        return new PodmanEnvironment(podmanPath, machine, host, connection);
    }

    private static IReadOnlyList<string> SplitPath(string? path)
        => string.IsNullOrWhiteSpace(path)
            ? Array.Empty<string>()
            : path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
}
