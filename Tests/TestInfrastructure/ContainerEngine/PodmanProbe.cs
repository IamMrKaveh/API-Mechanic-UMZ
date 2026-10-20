using System.Diagnostics;
using System.Text.Json;

namespace Tests.TestInfrastructure.ContainerEngine;

/// <summary>One Podman machine as reported by <c>podman machine inspect</c>.</summary>
internal sealed record PodmanMachine(string Name, string State, bool Rootful, string? PipePath, string? SocketPath);

/// <summary>Host information reported by <c>podman info --format "{{{{json .Host}}}}"</c>.</summary>
internal sealed record PodmanHost(string? RemoteSocket, bool Rootless);

/// <summary>One entry of <c>podman system connection list</c>.</summary>
internal sealed record PodmanConnection(string Name, string Uri, bool IsDefault);

/// <summary>Runs a command and returns its exit code plus combined output.</summary>
internal delegate (int ExitCode, string Output) ContainerCommand(string fileName, IReadOnlyList<string> arguments);

/// <summary>
/// Locates the Podman CLI and reads the Docker-compatible endpoint from it.
/// Pure parsing is separated from process execution so the logic is unit-testable
/// without Podman installed.
/// </summary>
internal static class PodmanProbe
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(15);

    /// <summary>Finds the Podman executable: explicit path, PATH, then well-known install locations.</summary>
    internal static string? FindPodman(
        string? configuredPath,
        bool isWindows,
        Func<string, bool> fileExists,
        IEnumerable<string> pathDirectories,
        IEnumerable<string> knownLocations)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return fileExists(configuredPath) ? configuredPath : null;

        var executableNames = isWindows ? new[] { "podman.exe" } : new[] { "podman" };

        foreach (var directory in pathDirectories.Where(directory => !string.IsNullOrWhiteSpace(directory)))
        {
            foreach (var executableName in executableNames)
            {
                var candidate = Path.Combine(directory.Trim(), executableName);
                if (fileExists(candidate))
                    return candidate;
            }
        }

        foreach (var candidate in knownLocations)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && fileExists(candidate))
                return candidate;
        }

        return null;
    }

    /// <summary>Default install locations for Podman when it is not on PATH.</summary>
    internal static IReadOnlyList<string> KnownLocations(bool isWindows)
    {
        if (isWindows)
        {
            return new[]
            {
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "RedHat",
                    "Podman",
                    "podman.exe"),
            };
        }

        return new[] { "/usr/bin/podman", "/usr/local/bin/podman", "/opt/homebrew/bin/podman" };
    }

    /// <summary>
    /// Reads the default Podman machine. Prefers the running machine; returns
    /// <c>null</c> when Podman has no machine (native Linux) or the command fails.
    /// </summary>
    internal static PodmanMachine? ReadMachine(ContainerCommand command, string podmanPath)
    {
        var formatted = command(podmanPath, new[] { "machine", "inspect", "--format", "{{json .}}" });
        if (formatted.ExitCode == 0 && TryParseMachine(formatted.Output, out var machine))
            return machine;

        var plain = command(podmanPath, new[] { "machine", "inspect" });
        if (plain.ExitCode != 0)
            return null;

        return ParseMachines(plain.Output);
    }

    /// <summary>Reads host socket/rootless information (used on native Linux Podman).</summary>
    internal static PodmanHost? ReadHost(ContainerCommand command, string podmanPath)
    {
        var result = command(podmanPath, new[] { "info", "--format", "{{json .Host}}" });
        if (result.ExitCode != 0 || !TryParseJson(result.Output, out var root))
            return null;

        var remoteSocket = FindNestedString(root, "remoteSocket", "path");
        var rootless = FindNestedBoolean(root, "security", "rootless") ?? false;

        return new PodmanHost(remoteSocket, rootless);
    }

    /// <summary>
    /// Reads the default entry of <c>podman system connection list</c>. On Windows this
    /// is the fallback for discovering the machine named pipe when
    /// <c>podman machine inspect</c> cannot create its lockfile (restricted environments).
    /// </summary>
    internal static PodmanConnection? ReadDefaultConnection(ContainerCommand command, string podmanPath)
    {
        var result = command(podmanPath, new[] { "system", "connection", "list", "--format", "{{json .}}" });
        if (result.ExitCode != 0)
            return null;

        return ParseConnections(result.Output);
    }

    internal static PodmanConnection? ParseConnections(string output)
    {
        var connections = new List<PodmanConnection>();

        if (TryParseJson(output, out var root))
        {
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in root.EnumerateArray())
                {
                    if (TryReadConnection(element, out var connection))
                        connections.Add(connection);
                }
            }
            else if (TryReadConnection(root, out var single))
            {
                connections.Add(single);
            }
        }

        if (connections.Count == 0)
        {
            // `podman system connection list --format` prints one JSON object per line.
            foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                if (TryParseJson(line.Trim(), out var element) && TryReadConnection(element, out var connection))
                    connections.Add(connection);
            }
        }

        return connections.FirstOrDefault(connection => connection.IsDefault)
               ?? connections.FirstOrDefault();
    }

    internal static PodmanMachine? ParseMachines(string json)
    {
        if (!TryParseJson(json, out var root))
            return null;

        if (root.ValueKind == JsonValueKind.Array)
        {
            var machines = new List<PodmanMachine>();
            foreach (var element in root.EnumerateArray())
            {
                if (TryReadMachine(element, out var machine))
                    machines.Add(machine);
            }

            return machines.FirstOrDefault(candidate =>
                       string.Equals(candidate.State, "running", StringComparison.OrdinalIgnoreCase))
                   ?? machines.FirstOrDefault();
        }

        return TryReadMachine(root, out var single) ? single : null;
    }

    /// <summary>Executes a real Podman CLI call with a bounded timeout.</summary>
    internal static (int ExitCode, string Output) Run(string fileName, IReadOnlyList<string> arguments)
    {
        try
        {
            var startInfo = new ProcessStartInfo(fileName)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            foreach (var argument in arguments)
                startInfo.ArgumentList.Add(argument);

            using var process = Process.Start(startInfo);
            if (process is null)
                return (-1, string.Empty);

            if (!process.WaitForExit((int)CommandTimeout.TotalMilliseconds))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Best effort: a stuck probe must not fail the test run.
                }

                return (-1, string.Empty);
            }

            var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
            return (process.ExitCode, output);
        }
        catch
        {
            return (-1, string.Empty);
        }
    }

    private static bool TryParseMachine(string output, out PodmanMachine machine)
    {
        machine = default!;
        if (!TryParseJson(output, out var root) || root.ValueKind != JsonValueKind.Object)
            return false;

        return TryReadMachine(root, out machine);
    }

    private static bool TryReadMachine(JsonElement element, out PodmanMachine machine)
    {
        machine = default!;
        if (element.ValueKind != JsonValueKind.Object)
            return false;

        var name = FindString(element, "Name") ?? "podman-machine-default";
        var state = FindString(element, "State") ?? string.Empty;
        var rootful = FindBoolean(element, "Rootful") ?? FindBoolean(element, "IsRootful") ?? false;
        var pipePath = FindNestedString(element, "ConnectionInfo", "PodmanPipe", "Path");
        var socketPath = FindNestedString(element, "ConnectionInfo", "PodmanSocket", "Path");

        machine = new PodmanMachine(name, state, rootful, pipePath, socketPath);
        return true;
    }

    private static bool TryReadConnection(JsonElement element, out PodmanConnection connection)
    {
        connection = default!;
        if (element.ValueKind != JsonValueKind.Object)
            return false;

        var name = FindString(element, "Name");
        var uri = FindString(element, "URI") ?? FindString(element, "Uri");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(uri))
            return false;

        var isDefault = FindBoolean(element, "Default") ?? FindBoolean(element, "IsDefault") ?? false;
        connection = new PodmanConnection(name, uri, isDefault);
        return true;
    }

    private static bool TryParseJson(string output, out JsonElement root)
    {
        root = default;
        if (string.IsNullOrWhiteSpace(output))
            return false;

        try
        {
            using var document = JsonDocument.Parse(output);
            root = document.RootElement.Clone();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static JsonElement? FindProperty(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                return property.Value;
        }

        return null;
    }

    private static string? FindString(JsonElement element, string name)
        => FindProperty(element, name) is { ValueKind: JsonValueKind.String } value
            ? value.GetString()
            : null;

    private static bool? FindBoolean(JsonElement element, string name)
    {
        var value = FindProperty(element, name);
        if (value is null)
            return null;

        return value.Value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    private static string? FindNestedString(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var name in path)
        {
            var next = FindProperty(current, name);
            if (next is null)
                return null;

            current = next.Value;
        }

        return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
    }

    private static bool? FindNestedBoolean(JsonElement element, params string[] path)
    {
        var current = element;
        foreach (var name in path)
        {
            var next = FindProperty(current, name);
            if (next is null)
                return null;

            current = next.Value;
        }

        return current.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }
}
