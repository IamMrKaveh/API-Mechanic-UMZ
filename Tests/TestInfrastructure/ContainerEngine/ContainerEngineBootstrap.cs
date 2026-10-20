using System.Runtime.CompilerServices;

namespace Tests.TestInfrastructure.ContainerEngine;

/// <summary>
/// Runs once when the test assembly is loaded (before any Testcontainers type is
/// used) and points Testcontainers at Podman when it is the available engine.
/// Explicit <c>DOCKER_HOST</c> values are never overwritten, so CI and developers
/// can always pin an endpoint.
/// </summary>
internal static class ContainerEngineBootstrap
{
    private const string DockerHostVariable = "DOCKER_HOST";
    private const string RyukDisabledVariable = "TESTCONTAINERS_RYUK_DISABLED";

    private static ContainerEngineDescriptor? _resolved;

    /// <summary>The engine selected by auto-detection, or <c>null</c> for the Testcontainers default.</summary>
    internal static ContainerEngineDescriptor? Resolved => _resolved;

    /// <summary>Human-readable status used in skip messages and test logs.</summary>
    internal static string DescribeConfiguration()
        => _resolved is null
            ? "Container engine: auto-detection found no running Podman (Docker/default endpoint in use)."
            : $"Container engine: {_resolved.Description}.";

    [ModuleInitializer]
    internal static void Initialize()
    {
        try
        {
            var isWindows = OperatingSystem.IsWindows();
            var dockerHost = Environment.GetEnvironmentVariable(DockerHostVariable);
            var preference = Environment.GetEnvironmentVariable(ContainerEngineResolver.EngineVariable);

            var podmanEnvironment = ProbePodman(isWindows, dockerHost, preference);
            var descriptor = ContainerEngineResolver.Resolve(preference, dockerHost, isWindows, podmanEnvironment);

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
            Console.Error.WriteLine($"[tests] container engine: {descriptor.Description}");
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
