using System.Runtime.CompilerServices;

namespace Tests.TestInfrastructure.ContainerEngine;

/// <summary>
/// Runs once when the test assembly is loaded (before any Testcontainers type is
/// used) and points Testcontainers at the best reachable container engine:
/// Podman first, then a Docker-compatible endpoint as fallback. Explicit
/// <c>DOCKER_HOST</c> values are never overwritten.
/// </summary>
internal static class ContainerEngineBootstrap
{
    private const string DockerHostVariable = "DOCKER_HOST";
    private const string RyukDisabledVariable = "TESTCONTAINERS_RYUK_DISABLED";
    private const string WindowsDockerPipe = "npipe://./pipe/docker_engine";
    private const string UnixDockerSocket = "unix:///var/run/docker.sock";

    private static ContainerEngineDescriptor? _resolved;
    private static bool _reachable;

    /// <summary>The engine selected by auto-detection, or <c>null</c> for the Testcontainers default.</summary>
    internal static ContainerEngineDescriptor? Resolved => _resolved;

    /// <summary>Whether the selected endpoint answered a reachability probe.</summary>
    internal static bool IsReachable => _reachable;

    /// <summary>Human-readable status used in skip messages and test logs.</summary>
    internal static string DescribeConfiguration()
    {
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
            var preference = Environment.GetEnvironmentVariable(ContainerEngineResolver.EngineVariable);

            var podmanEnvironment = ProbePodman(isWindows, dockerHost, preference);
            var candidates = ContainerEngineResolver.ResolveCandidates(
                preference, dockerHost, isWindows, podmanEnvironment);

            ContainerEngineDescriptor? descriptor = null;

            // A running Podman machine may expose several endpoints; a pipe can be
            // denied for this process even when the machine is healthy, so probe.
            foreach (var candidate in candidates)
            {
                if (!ContainerEngineEndpointProbe.IsReachable(candidate.DockerHost))
                    continue;

                descriptor = candidate;
                _reachable = true;
                break;
            }

            // Docker Desktop (or Podman Desktop's Docker-compatibility pipe) can still
            // be reachable when Podman's own pipe is denied.
            var allowDockerFallback = !string.Equals(preference, "podman", StringComparison.OrdinalIgnoreCase);
            if (!_reachable && allowDockerFallback)
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

            // Keep the primary Podman endpoint even when unreachable: the skip message
            // then names the intended engine instead of a generic Docker error.
            descriptor ??= candidates.FirstOrDefault();

            if (descriptor is null)
                return;

            Environment.SetEnvironmentVariable(DockerHostVariable, descriptor.DockerHost);

            // Rootless Podman cannot mount its own socket into the Ryuk container,
            // so the reaper is disabled unless the developer set the variable.
            if (descriptor.DisableRyuk
                && string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(RyukDisabledVariable)))
            {
                Environment.SetEnvironmentVariable(RyukDisabledVariable, "true");
            }

            _resolved = descriptor;
            Console.Error.WriteLine($"[tests] container engine: {DescribeConfiguration()}");
        }
        catch
        {
            // Engine detection must never break test discovery or execution.
        }
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
