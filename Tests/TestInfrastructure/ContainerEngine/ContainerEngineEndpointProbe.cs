using System.IO.Pipes;
using System.Net.Sockets;

namespace Tests.TestInfrastructure.ContainerEngine;

/// <summary>
/// Cheap reachability probe for container endpoints. The Windows Podman pipe can
/// be denied for the test process (different elevation/session, hardened pipe
/// ACL, sandbox), while Docker Desktop's or Podman Desktop's Docker-compatible
/// <c>docker_engine</c> pipe may still be reachable - so the bootstrap probes the
/// candidates before pinning one.
/// </summary>
internal static class ContainerEngineEndpointProbe
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMilliseconds(1500);

    internal static bool IsReachable(string endpoint, TimeSpan? timeout = null)
    {
        var effectiveTimeout = timeout ?? DefaultTimeout;

        try
        {
            if (endpoint.StartsWith("npipe://", StringComparison.OrdinalIgnoreCase))
                return IsNamedPipeReachable(endpoint, effectiveTimeout);

            if (endpoint.StartsWith("unix://", StringComparison.OrdinalIgnoreCase))
                return IsUnixSocketReachable(endpoint, effectiveTimeout);

            if (endpoint.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase))
                return IsTcpReachable(endpoint, effectiveTimeout);
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool IsNamedPipeReachable(string endpoint, TimeSpan timeout)
    {
        var uri = new Uri(endpoint);
        var pipeName = uri.AbsolutePath.Trim('/');
        const string pipePrefix = "pipe/";

        if (!pipeName.StartsWith(pipePrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        pipeName = pipeName[pipePrefix.Length..];

        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
        client.Connect((int)timeout.TotalMilliseconds);
        return client.IsConnected;
    }

    private static bool IsUnixSocketReachable(string endpoint, TimeSpan timeout)
    {
        var path = endpoint["unix://".Length..];
        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        var connect = socket.ConnectAsync(new UnixDomainSocketEndPoint(path));
        return connect.Wait(timeout) && socket.Connected;
    }

    private static bool IsTcpReachable(string endpoint, TimeSpan timeout)
    {
        var uri = new Uri(endpoint);
        using var client = new TcpClient();
        var connect = client.ConnectAsync(uri.Host, uri.Port);
        return connect.Wait(timeout) && client.Connected;
    }
}
