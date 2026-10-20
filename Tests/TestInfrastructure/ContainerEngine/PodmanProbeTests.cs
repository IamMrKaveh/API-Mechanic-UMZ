namespace Tests.TestInfrastructure.ContainerEngine;

public class PodmanProbeTests
{
    private const string RunningMachineJson = """
        {
          "ConfigDir": { "Path": "C:\\Users\\dev\\.config\\containers" },
          "ConnectionInfo": {
            "PodmanPipe": { "Path": "\\\\.\\pipe\\podman-machine-default" },
            "PodmanSocket": { "Path": "C:\\Users\\dev\\AppData\\Local\\Temp\\podman\\podman-machine-default-api.sock" }
          },
          "Name": "podman-machine-default",
          "Rootful": false,
          "State": "running"
        }
        """;

    private const string MachinesArrayJson = """
        [
          {
            "ConnectionInfo": { "PodmanPipe": { "Path": "\\\\.\\pipe\\stopped-machine" } },
            "Name": "stopped-machine",
            "Rootful": true,
            "State": "exited"
          },
          {
            "ConnectionInfo": {
              "PodmanPipe": { "Path": "\\\\.\\pipe\\podman-machine-default" },
              "PodmanSocket": { "Path": "C:\\Temp\\podman.sock" }
            },
            "Name": "podman-machine-default",
            "Rootful": false,
            "State": "running"
          }
        ]
        """;

    private const string HostJson = """
        {
          "remoteSocket": { "path": "unix:///run/user/1000/podman/podman.sock", "exists": true },
          "security": { "rootless": true }
        }
        """;

    private const string ConnectionsJsonLines = """
        {"Name":"podman-machine-default","URI":"ssh://user@127.0.0.1:49673/run/user/1000/podman/podman.sock","Default":true,"ReadWrite":true}
        {"Name":"podman-machine-default-root","URI":"ssh://root@127.0.0.1:49673/run/podman/podman.sock","Default":false,"ReadWrite":true}
        """;

    [Fact]
    public void ReadMachine_ParsesTheFormattedInspectObject()
    {
        ContainerCommand command = (_, _) => (0, RunningMachineJson);

        var machine = PodmanProbe.ReadMachine(command, "/usr/bin/podman");

        machine.ShouldNotBeNull();
        machine!.Name.ShouldBe("podman-machine-default");
        machine.State.ShouldBe("running");
        machine.Rootful.ShouldBeFalse();
        machine.PipePath.ShouldBe(@"\\.\pipe\podman-machine-default");
        machine.SocketPath.ShouldBe(
            @"C:\Users\dev\AppData\Local\Temp\podman\podman-machine-default-api.sock");
    }

    [Fact]
    public void ReadMachine_FallsBackToTheArrayAndPrefersTheRunningMachine()
    {
        ContainerCommand command = (_, arguments) => arguments.Contains("--format")
            ? (1, string.Empty)
            : (0, MachinesArrayJson);

        var machine = PodmanProbe.ReadMachine(command, "/usr/bin/podman");

        machine.ShouldNotBeNull();
        machine!.Name.ShouldBe("podman-machine-default");
        machine.State.ShouldBe("running");
    }

    [Fact]
    public void ReadMachine_ReturnsNullWhenBothCommandsFail()
    {
        ContainerCommand command = (_, _) => (1, string.Empty);

        PodmanProbe.ReadMachine(command, "/usr/bin/podman").ShouldBeNull();
    }

    [Fact]
    public void ReadMachine_ReturnsNullForInvalidJson()
    {
        ContainerCommand command = (_, _) => (0, "not-json");

        PodmanProbe.ReadMachine(command, "/usr/bin/podman").ShouldBeNull();
    }

    [Fact]
    public void ReadHost_ParsesTheRemoteSocketAndRootlessFlag()
    {
        ContainerCommand command = (_, _) => (0, HostJson);

        var host = PodmanProbe.ReadHost(command, "/usr/bin/podman");

        host.ShouldNotBeNull();
        host!.RemoteSocket.ShouldBe("unix:///run/user/1000/podman/podman.sock");
        host.Rootless.ShouldBeTrue();
    }

    [Fact]
    public void ReadHost_ReturnsNullWhenPodmanInfoFails()
    {
        ContainerCommand command = (_, _) => (125, "Error: cannot connect");

        PodmanProbe.ReadHost(command, "/usr/bin/podman").ShouldBeNull();
    }

    [Fact]
    public void ReadDefaultConnection_ParsesTheJsonLinesOutput()
    {
        ContainerCommand command = (_, _) => (0, ConnectionsJsonLines);

        var connection = PodmanProbe.ReadDefaultConnection(command, "/usr/bin/podman");

        connection.ShouldNotBeNull();
        connection!.Name.ShouldBe("podman-machine-default");
        connection.Uri.ShouldBe("ssh://user@127.0.0.1:49673/run/user/1000/podman/podman.sock");
        connection.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void ReadDefaultConnection_ParsesAnArrayAndPrefersTheDefaultEntry()
    {
        const string output = """
            [
              { "Name": "secondary", "URI": "unix:///run/user/1000/podman/secondary.sock", "Default": false },
              { "Name": "primary", "URI": "unix:///run/user/1000/podman/podman.sock", "Default": true }
            ]
            """;

        var connection = PodmanProbe.ParseConnections(output);

        connection.ShouldNotBeNull();
        connection!.Name.ShouldBe("primary");
    }

    [Fact]
    public void ReadDefaultConnection_ReturnsNullWhenTheCommandFails()
    {
        ContainerCommand command = (_, _) => (125, "Error: cannot connect");

        PodmanProbe.ReadDefaultConnection(command, "/usr/bin/podman").ShouldBeNull();
    }

    [Fact]
    public void ReadDefaultConnection_ReturnsNullForInvalidJson()
    {
        ContainerCommand command = (_, _) => (0, "no connections");

        PodmanProbe.ReadDefaultConnection(command, "/usr/bin/podman").ShouldBeNull();
    }

    [Fact]
    public void FindPodman_UsesTheConfiguredPathWhenItExists()
    {
        var configured = OperatingSystem.IsWindows() ? @"C:\Tools\podman.exe" : "/opt/tools/podman";

        var result = PodmanProbe.FindPodman(
            configured,
            OperatingSystem.IsWindows(),
            path => string.Equals(path, configured, StringComparison.Ordinal),
            Array.Empty<string>(),
            Array.Empty<string>());

        result.ShouldBe(configured);
    }

    [Fact]
    public void FindPodman_ReturnsNullWhenTheConfiguredPathIsMissing()
    {
        var result = PodmanProbe.FindPodman(
            "/missing/podman",
            false,
            _ => false,
            Array.Empty<string>(),
            Array.Empty<string>());

        result.ShouldBeNull();
    }

    [Fact]
    public void FindPodman_FallsBackToPathDirectories()
    {
        var directory = OperatingSystem.IsWindows() ? @"C:\tools" : "/tools";
        var podmanName = OperatingSystem.IsWindows() ? "podman.exe" : "podman";
        var expected = Path.Combine(directory, podmanName);

        var result = PodmanProbe.FindPodman(
            null,
            OperatingSystem.IsWindows(),
            path => string.Equals(path, expected, StringComparison.Ordinal),
            new[] { directory },
            Array.Empty<string>());

        result.ShouldBe(expected);
    }

    [Fact]
    public void FindPodman_FallsBackToKnownInstallLocations()
    {
        var known = OperatingSystem.IsWindows()
            ? @"C:\Program Files\RedHat\Podman\podman.exe"
            : "/usr/bin/podman";

        var result = PodmanProbe.FindPodman(
            null,
            OperatingSystem.IsWindows(),
            path => string.Equals(path, known, StringComparison.Ordinal),
            Array.Empty<string>(),
            new[] { known });

        result.ShouldBe(known);
    }

    [Fact]
    public void FindPodman_ReturnsNullWhenNothingExists()
    {
        var result = PodmanProbe.FindPodman(
            null,
            false,
            _ => false,
            new[] { "/nope" },
            new[] { "/nope/podman" });

        result.ShouldBeNull();
    }
}
