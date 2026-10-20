using System.Net;
using System.Net.Sockets;

namespace Tests.TestInfrastructure.ContainerEngine;

public class ContainerEngineEndpointProbeTests
{
    [Fact]
    public void IsReachable_WithClosedTcpPort_ReturnsFalse()
    {
        // Bind a listener, take its port, then stop it so the port is closed.
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        var reachable = ContainerEngineEndpointProbe.IsReachable(
            $"tcp://127.0.0.1:{port}", TimeSpan.FromMilliseconds(500));

        reachable.ShouldBeFalse();
    }

    [Fact]
    public async Task IsReachable_WithListeningTcpPort_ReturnsTrue()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;

            // Accept asynchronously so the probe's connect completes.
            var accept = listener.AcceptSocketAsync();

            var reachable = ContainerEngineEndpointProbe.IsReachable(
                $"tcp://127.0.0.1:{port}", TimeSpan.FromSeconds(2));

            reachable.ShouldBeTrue();
            await accept.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public void IsReachable_WithMissingNamedPipe_ReturnsFalse()
    {
        var reachable = ContainerEngineEndpointProbe.IsReachable(
            $"npipe://./pipe/mechanic-test-missing-{Guid.NewGuid():N}",
            TimeSpan.FromMilliseconds(500));

        reachable.ShouldBeFalse();
    }

    [Fact]
    public void IsReachable_WithMissingUnixSocket_ReturnsFalse()
    {
        var path = Path.Combine(Path.GetTempPath(), $"mechanic-test-{Guid.NewGuid():N}.sock");

        var reachable = ContainerEngineEndpointProbe.IsReachable(
            $"unix://{path}", TimeSpan.FromMilliseconds(500));

        reachable.ShouldBeFalse();
    }

    [Fact]
    public void IsReachable_WithUnsupportedScheme_ReturnsFalse()
    {
        ContainerEngineEndpointProbe.IsReachable("ssh://user@host/socket").ShouldBeFalse();
    }
}
