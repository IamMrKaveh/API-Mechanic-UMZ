namespace Tests.TestInfrastructure.ContainerEngine;

/// <summary>Everything the resolver needs to know about the local Podman installation.</summary>
internal sealed record PodmanEnvironment(
    string PodmanPath,
    PodmanMachine? Machine,
    PodmanHost? Host,
    PodmanConnection? DefaultConnection);

/// <summary>
/// Decides which container endpoint the tests should use. Docker stays the
/// default; Podman is selected when it is installed and an endpoint is
/// discoverable, unless the developer pinned an endpoint via <c>DOCKER_HOST</c>
/// or disabled Podman via <c>MECHANIC_CONTAINER_ENGINE=docker</c>.
/// </summary>
internal static class ContainerEngineResolver
{
    /// <summary>Optional preference: <c>auto</c> (default), <c>podman</c> or <c>docker</c>.</summary>
    internal const string EngineVariable = "MECHANIC_CONTAINER_ENGINE";

    /// <summary>Optional explicit path to the Podman executable.</summary>
    internal const string PodmanPathVariable = "PODMAN_PATH";

    internal const string DefaultMachineName = "podman-machine-default";

    internal static ContainerEngineDescriptor? Resolve(
        string? enginePreference,
        string? dockerHost,
        bool isWindows,
        PodmanEnvironment? podman)
    {
        if (string.Equals(enginePreference, "docker", StringComparison.OrdinalIgnoreCase))
            return null;

        // An explicit endpoint always wins: the developer (or CI) already decided.
        if (!string.IsNullOrWhiteSpace(dockerHost))
            return null;

        if (podman is null || string.IsNullOrWhiteSpace(podman.PodmanPath))
            return null;

        // A running machine is the most precise source (endpoint + rootful flag).
        if (podman.Machine is { } machine
            && string.Equals(machine.State, "running", StringComparison.OrdinalIgnoreCase))
        {
            var machineEndpoint = isWindows
                ? ToNamedPipeEndpoint(machine.PipePath)
                : ToUnixEndpoint(machine.SocketPath);

            if (machineEndpoint is not null)
                return Build(machineEndpoint, !machine.Rootful, $"Podman machine '{machine.Name}'");
        }

        var rootless = podman.Host?.Rootless ?? false;

        if (isWindows)
        {
            // `podman info` reports the socket *inside* the WSL machine, which is not
            // reachable from Windows. The Windows endpoint is a named pipe named after
            // the default Podman machine/connection.
            if (podman.Host is null)
                return null;

            var name = podman.DefaultConnection?.Name
                       ?? podman.Machine?.Name
                       ?? DefaultMachineName;

            return Build(
                $"npipe://./pipe/{name}",
                rootless,
                $"Podman machine '{name}' (from podman system connection)");
        }

        // Native Linux / macOS without a machine: use the socket Podman reports.
        if (IsSupportedEndpoint(podman.Host?.RemoteSocket))
            return Build(podman.Host!.RemoteSocket!, rootless, "Podman host socket");

        if (IsSupportedEndpoint(podman.DefaultConnection?.Uri))
            return Build(podman.DefaultConnection!.Uri, rootless, $"Podman connection '{podman.DefaultConnection.Name}'");

        return null;
    }

    /// <summary>
    /// Converts a Windows pipe path (<c>\\.\pipe\podman-machine-default</c>) into the
    /// Testcontainers URI form (<c>npipe://./pipe/podman-machine-default</c>).
    /// </summary>
    internal static string? ToNamedPipeEndpoint(string? pipePath)
    {
        if (string.IsNullOrWhiteSpace(pipePath))
            return null;

        var normalized = pipePath.Replace('\\', '/').TrimEnd('/');
        var markerIndex = normalized.IndexOf("/pipe/", StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
            return null;

        var pipeName = normalized[(markerIndex + "/pipe/".Length)..].Trim('/');
        return string.IsNullOrWhiteSpace(pipeName) ? null : $"npipe://./pipe/{pipeName}";
    }

    /// <summary>Builds a Testcontainers <c>unix://</c> endpoint from a socket path.</summary>
    internal static string? ToUnixEndpoint(string? socketPath)
    {
        if (string.IsNullOrWhiteSpace(socketPath))
            return null;

        var normalized = socketPath.Replace('\\', '/');

        if (normalized.StartsWith("unix://", StringComparison.OrdinalIgnoreCase))
            return normalized;

        // "/run/user/1000/podman/podman.sock" -> "unix:///run/user/1000/podman/podman.sock"
        // "C:/Users/.../podman.sock"           -> "unix://C:/Users/.../podman.sock"
        return "unix://" + normalized;
    }

    private static ContainerEngineDescriptor Build(string endpoint, bool rootless, string source)
    {
        var description =
            $"{source} via {endpoint} ({(rootless ? "rootless" : "rootful")}" +
            $"{(rootless ? ", Ryuk disabled" : string.Empty)})";

        return new ContainerEngineDescriptor(ContainerEngineKind.Podman, endpoint, rootless, rootless, description);
    }

    private static bool IsSupportedEndpoint(string? endpoint)
        => !string.IsNullOrWhiteSpace(endpoint)
           && (endpoint.StartsWith("unix://", StringComparison.OrdinalIgnoreCase)
               || endpoint.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase));
}
