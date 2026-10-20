namespace Tests.TestInfrastructure.ContainerEngine;

public class ContainerEngineResolverTests
{
    private const string PodmanPath = "/usr/bin/podman";
    private const string WindowsPipe = @"\\.\pipe\podman-machine-default";
    private const string LinuxSocket = "/run/user/1000/podman/podman.sock";

    private static PodmanEnvironment Environment(
        PodmanMachine? machine = null,
        PodmanHost? host = null,
        PodmanConnection? connection = null,
        string podmanPath = PodmanPath)
        => new(podmanPath, machine, host, connection);

    private static PodmanMachine Machine(
        bool rootful = false,
        string state = "running",
        string? pipe = WindowsPipe,
        string? socket = LinuxSocket)
        => new("podman-machine-default", state, rootful, pipe, socket);

    [Fact]
    public void Resolve_WithExplicitDockerHost_KeepsTheDeveloperEndpoint()
    {
        var descriptor = ContainerEngineResolver.Resolve(
            enginePreference: null,
            dockerHost: "tcp://127.0.0.1:2375",
            isWindows: true,
            podman: Environment(machine: Machine()));

        descriptor.ShouldBeNull();
    }

    [Fact]
    public void Resolve_WithDockerPreference_DoesNotSelectPodman()
    {
        var descriptor = ContainerEngineResolver.Resolve(
            enginePreference: "docker",
            dockerHost: null,
            isWindows: true,
            podman: Environment(machine: Machine()));

        descriptor.ShouldBeNull();
    }

    [Fact]
    public void Resolve_WithoutPodman_ReturnsNull()
    {
        var descriptor = ContainerEngineResolver.Resolve(
            enginePreference: null,
            dockerHost: null,
            isWindows: false,
            podman: Environment(podmanPath: string.Empty));

        descriptor.ShouldBeNull();
    }

    [Fact]
    public void Resolve_WithoutPodmanEnvironment_ReturnsNull()
    {
        ContainerEngineResolver.Resolve(null, null, false, null).ShouldBeNull();
    }

    [Fact]
    public void Resolve_RunningRootlessMachineOnWindows_UsesNamedPipeAndDisablesRyuk()
    {
        var descriptor = ContainerEngineResolver.Resolve(
            enginePreference: "podman",
            dockerHost: null,
            isWindows: true,
            podman: Environment(machine: Machine(rootful: false)));

        descriptor.ShouldNotBeNull();
        descriptor!.Kind.ShouldBe(ContainerEngineKind.Podman);
        descriptor.DockerHost.ShouldBe("npipe://./pipe/podman-machine-default");
        descriptor.IsRootless.ShouldBeTrue();
        descriptor.DisableRyuk.ShouldBeTrue();
        descriptor.Description.ShouldContain("rootless");
    }

    [Fact]
    public void Resolve_RunningRootfulMachine_KeepsRyukEnabled()
    {
        var descriptor = ContainerEngineResolver.Resolve(
            enginePreference: null,
            dockerHost: null,
            isWindows: true,
            podman: Environment(machine: Machine(rootful: true)));

        descriptor.ShouldNotBeNull();
        descriptor!.IsRootless.ShouldBeFalse();
        descriptor.DisableRyuk.ShouldBeFalse();
    }

    [Fact]
    public void Resolve_WindowsMachineWithoutPipe_FallsBackToTheConnectionName()
    {
        var descriptor = ContainerEngineResolver.Resolve(
            enginePreference: null,
            dockerHost: null,
            isWindows: true,
            podman: Environment(
                machine: Machine(pipe: null),
                host: new PodmanHost(LinuxSocket, Rootless: true),
                connection: new PodmanConnection("podman-machine-default", "ssh://user@host/socket", IsDefault: true)));

        descriptor.ShouldNotBeNull();
        descriptor!.DockerHost.ShouldBe("npipe://./pipe/podman-machine-default");
        descriptor.IsRootless.ShouldBeTrue();
    }

    [Fact]
    public void Resolve_StoppedMachineOnWindowsStillUsesTheDefaultPipe()
    {
        var descriptor = ContainerEngineResolver.Resolve(
            enginePreference: null,
            dockerHost: null,
            isWindows: true,
            podman: Environment(
                machine: Machine(state: "exited"),
                host: new PodmanHost(LinuxSocket, Rootless: true),
                connection: new PodmanConnection("podman-machine-default", "ssh://user@host/socket", IsDefault: true)));

        descriptor.ShouldNotBeNull();
        descriptor!.DockerHost.ShouldBe("npipe://./pipe/podman-machine-default");
    }

    [Fact]
    public void ResolveCandidates_WindowsRootless_ReturnsMachinePipeFirstAndDeduplicates()
    {
        var candidates = ContainerEngineResolver.ResolveCandidates(
            enginePreference: null,
            dockerHost: null,
            isWindows: true,
            podman: Environment(
                machine: Machine(rootful: false),
                host: new PodmanHost(LinuxSocket, Rootless: true),
                connection: new PodmanConnection("podman-machine-default", "ssh://user@host/socket", IsDefault: true)));

        candidates.Count.ShouldBe(1);
        candidates[0].DockerHost.ShouldBe("npipe://./pipe/podman-machine-default");
        candidates[0].DisableRyuk.ShouldBeTrue();
    }

    [Fact]
    public void ResolveCandidates_Unix_IncludesMachineAndHostEndpointsInOrder()
    {
        var candidates = ContainerEngineResolver.ResolveCandidates(
            enginePreference: null,
            dockerHost: null,
            isWindows: false,
            podman: Environment(
                machine: Machine(socket: LinuxSocket),
                host: new PodmanHost("unix:///run/user/1000/podman/podman.sock", Rootless: true)));

        candidates.Count.ShouldBe(1, "an identical socket must be deduplicated");
        candidates[0].DockerHost.ShouldBe("unix:///run/user/1000/podman/podman.sock");
    }

    [Fact]
    public void ResolveCandidates_WithoutMachine_ReturnsHostAndConnectionCandidates()
    {
        var candidates = ContainerEngineResolver.ResolveCandidates(
            enginePreference: null,
            dockerHost: null,
            isWindows: false,
            podman: Environment(
                host: new PodmanHost("unix:///run/user/1000/podman/podman.sock", Rootless: true),
                connection: new PodmanConnection("rootful", "unix:///run/podman/podman.sock", IsDefault: true)));

        candidates.Count.ShouldBe(2);
        candidates[0].DockerHost.ShouldBe("unix:///run/user/1000/podman/podman.sock");
        candidates[1].DockerHost.ShouldBe("unix:///run/podman/podman.sock");
    }

    [Fact]
    public void ResolveCandidates_WithDockerHostConfigured_ReturnsNone()
    {
        ContainerEngineResolver.ResolveCandidates(
                enginePreference: null,
                dockerHost: "tcp://127.0.0.1:2375",
                isWindows: true,
                podman: Environment(machine: Machine()))
            .ShouldBeEmpty();
    }

    [Fact]
    public void Resolve_WindowsWithoutReachableEngine_ReturnsNull()
    {
        // `podman info` failing means no running machine, so a pipe would be misleading.
        var descriptor = ContainerEngineResolver.Resolve(
            enginePreference: null,
            dockerHost: null,
            isWindows: true,
            podman: Environment(
                machine: Machine(state: "exited"),
                host: null,
                connection: new PodmanConnection("podman-machine-default", "ssh://user@host/socket", IsDefault: true)));

        descriptor.ShouldBeNull();
    }

    [Fact]
    public void Resolve_StoppedMachineWithoutHostOrConnection_ReturnsNull()
    {
        var descriptor = ContainerEngineResolver.Resolve(
            enginePreference: null,
            dockerHost: null,
            isWindows: true,
            podman: Environment(machine: Machine(state: "exited")));

        descriptor.ShouldBeNull();
    }

    [Fact]
    public void Resolve_LinuxMachine_UsesUnixSocket()
    {
        var descriptor = ContainerEngineResolver.Resolve(
            enginePreference: null,
            dockerHost: null,
            isWindows: false,
            podman: Environment(machine: Machine(socket: LinuxSocket)));

        descriptor.ShouldNotBeNull();
        descriptor!.DockerHost.ShouldBe("unix:///run/user/1000/podman/podman.sock");
        descriptor.IsRootless.ShouldBeTrue();
    }

    [Fact]
    public void Resolve_NativeLinuxPodmanWithoutMachine_UsesHostRemoteSocket()
    {
        var descriptor = ContainerEngineResolver.Resolve(
            enginePreference: null,
            dockerHost: null,
            isWindows: false,
            podman: Environment(host: new PodmanHost("unix:///run/user/1000/podman/podman.sock", Rootless: true)));

        descriptor.ShouldNotBeNull();
        descriptor!.DockerHost.ShouldBe("unix:///run/user/1000/podman/podman.sock");
        descriptor.DisableRyuk.ShouldBeTrue();
    }

    [Fact]
    public void Resolve_NativeLinuxWithoutHost_UsesAUnixConnectionUri()
    {
        var descriptor = ContainerEngineResolver.Resolve(
            enginePreference: null,
            dockerHost: null,
            isWindows: false,
            podman: Environment(
                connection: new PodmanConnection(
                    "rootful",
                    "unix:///run/podman/podman.sock",
                    IsDefault: true)));

        descriptor.ShouldNotBeNull();
        descriptor!.DockerHost.ShouldBe("unix:///run/podman/podman.sock");
        descriptor.IsRootless.ShouldBeFalse();
    }

    [Fact]
    public void Resolve_UnsupportedSshConnectionWithoutHost_ReturnsNull()
    {
        var descriptor = ContainerEngineResolver.Resolve(
            enginePreference: null,
            dockerHost: null,
            isWindows: false,
            podman: Environment(
                connection: new PodmanConnection(
                    "remote",
                    "ssh://user@remote:49673/run/user/1000/podman/podman.sock",
                    IsDefault: true)));

        descriptor.ShouldBeNull();
    }

    [Theory]
    [InlineData(@"\\.\pipe\podman-machine-default", "npipe://./pipe/podman-machine-default")]
    [InlineData("/./pipe/podman", "npipe://./pipe/podman")]
    [InlineData(@"\\.\pipe\", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void ToNamedPipeEndpoint_NormalizesWindowsPipePaths(string? pipePath, string? expected)
    {
        ContainerEngineResolver.ToNamedPipeEndpoint(pipePath).ShouldBe(expected);
    }

    [Theory]
    [InlineData("/run/user/1000/podman/podman.sock", "unix:///run/user/1000/podman/podman.sock")]
    [InlineData(@"C:\Users\dev\podman-machine-default-api.sock", "unix://C:/Users/dev/podman-machine-default-api.sock")]
    [InlineData("unix:///already/a/url.sock", "unix:///already/a/url.sock")]
    [InlineData(null, null)]
    public void ToUnixEndpoint_NormalizesSocketPaths(string? socketPath, string? expected)
    {
        ContainerEngineResolver.ToUnixEndpoint(socketPath).ShouldBe(expected);
    }
}
