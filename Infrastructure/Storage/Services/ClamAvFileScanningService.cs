using System.Buffers.Binary;
using System.Net.Sockets;
using Application.Storage.Contracts;
using Infrastructure.Storage.Options;

namespace Infrastructure.Storage.Services;

public sealed class ClamAvFileScanningService(
    IOptions<AntivirusOptions> options,
    IAuditService auditService) : IFileScanningService
{
    private readonly AntivirusOptions _options = options.Value;

    public async Task<FileScanResult> ScanAsync(Stream stream, string fileName, CancellationToken ct = default)
    {
        if (!_options.IsEnabled)
            return FileScanResult.Clean();

        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        if (stream.CanSeek)
            stream.Position = 0;

        using var tcp = new TcpClient();
        tcp.ReceiveTimeout = _options.TimeoutSeconds * 1000;
        tcp.SendTimeout = _options.TimeoutSeconds * 1000;

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

            await tcp.ConnectAsync(_options.Host, _options.Port, cts.Token);
            await using var netStream = tcp.GetStream();

            var instream = "zINSTREAM\0"u8.ToArray();
            await netStream.WriteAsync(instream, cts.Token);

            var chunkBuffer = new byte[_options.ChunkSizeBytes];
            var lengthBuffer = new byte[4];

            int read;
            while ((read = await stream.ReadAsync(chunkBuffer.AsMemory(0, chunkBuffer.Length), cts.Token)) > 0)
            {
                BinaryPrimitives.WriteUInt32BigEndian(lengthBuffer, (uint)read);
                await netStream.WriteAsync(lengthBuffer, cts.Token);
                await netStream.WriteAsync(chunkBuffer.AsMemory(0, read), cts.Token);
            }

            BinaryPrimitives.WriteUInt32BigEndian(lengthBuffer, 0);
            await netStream.WriteAsync(lengthBuffer, cts.Token);
            await netStream.FlushAsync(cts.Token);

            using var responseBuffer = new MemoryStream();
            var readBuffer = new byte[512];
            while (true)
            {
                var bytes = await netStream.ReadAsync(readBuffer.AsMemory(), cts.Token);
                if (bytes <= 0) break;
                responseBuffer.Write(readBuffer, 0, bytes);
                if (readBuffer.AsSpan(0, bytes).IndexOf((byte)0) >= 0) break;
            }

            if (stream.CanSeek) stream.Position = 0;

            var response = System.Text.Encoding.ASCII.GetString(responseBuffer.ToArray()).TrimEnd('\0', '\n', '\r', ' ');

            if (response.EndsWith("OK", StringComparison.Ordinal))
                return FileScanResult.Clean();

            if (response.EndsWith("FOUND", StringComparison.Ordinal))
            {
                var parts = response.Split(':', 2, StringSplitOptions.TrimEntries);
                var body = parts.Length == 2 ? parts[1] : response;
                var threat = body.Replace("FOUND", string.Empty, StringComparison.Ordinal).Trim();

                await auditService.LogSecurityEventAsync(
                    "MaliciousUploadDetected",
                    $"ClamAV detected threat '{threat}' in file '{fileName}'.",
                    IpAddress.Unknown,
                    null,
                    ct);

                return FileScanResult.Infected(threat, response);
            }

            await auditService.LogErrorAsync(
                $"[ClamAV] Unexpected engine response for '{fileName}': {response}", ct);

            return _options.FailClosedOnEngineError
                ? FileScanResult.Infected("ClamAV.EngineError", response)
                : FileScanResult.Clean();
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            await auditService.LogErrorAsync(
                $"[ClamAV] Scan timed out for '{fileName}' after {_options.TimeoutSeconds}s.", ct);
            return _options.FailClosedOnEngineError
                ? FileScanResult.Infected("ClamAV.Timeout", null)
                : FileScanResult.Clean();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await auditService.LogErrorAsync(
                $"[ClamAV] Scan failed for '{fileName}': {ex.GetType().Name}: {ex.Message}", ct);
            return _options.FailClosedOnEngineError
                ? FileScanResult.Infected("ClamAV.Unavailable", ex.Message)
                : FileScanResult.Clean();
        }
    }
}
