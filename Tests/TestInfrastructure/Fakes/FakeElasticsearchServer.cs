using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Tests.TestInfrastructure.Fakes;

public sealed class FakeElasticsearchServer : IAsyncDisposable
{
    private readonly TcpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;

    public string BaseUrl { get; }

    public List<(string Method, string Path, string Body)> Requests { get; } = new();

    public Func<string, string, (string Body, int Status)> Router { get; set; } =
        (_, _) => ("""{"errors":false,"took":1,"items":[]}""", 200);

    public FakeElasticsearchServer()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        var port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        BaseUrl = $"http://127.0.0.1:{port}";
        _loop = Task.Run(AcceptLoopAsync);
    }

    public Elastic.Clients.Elasticsearch.ElasticsearchClient CreateClient() =>
        new(new Uri(BaseUrl));

    private async Task AcceptLoopAsync()
    {
        while (!_cts.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            _ = Task.Run(() => HandleAsync(client), _cts.Token);
        }
    }

    private async Task HandleAsync(TcpClient client)
    {
        using (client)
        {
            var stream = client.GetStream();
            // Latin1 maps bytes 1:1 to chars, so buffered binary bodies survive exactly.
            using var reader = new StreamReader(stream, Encoding.Latin1, false, 1024, leaveOpen: true);
            var requestLine = await reader.ReadLineAsync() ?? string.Empty;
            var parts = requestLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var method = parts.Length > 0 ? parts[0] : "GET";
            var path = parts.Length > 1 ? parts[1] : "/";

            string? line;
            var contentLength = 0;
            var contentEncoding = string.Empty;
            while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
            {
                var idx = line.IndexOf(':');
                if (idx <= 0) continue;
                var headerName = line[..idx].Trim();
                var headerValue = line[(idx + 1)..].Trim();
                if (headerName.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    int.TryParse(headerValue, out contentLength);
                else if (headerName.Equals("Content-Encoding", StringComparison.OrdinalIgnoreCase))
                    contentEncoding = headerValue;
            }

            var body = string.Empty;
            if (contentLength > 0)
            {
                // Read raw bytes: the client may gzip request bodies.
                var buf = new char[contentLength];
                var read = 0;
                while (read < contentLength)
                {
                    var n = await reader.ReadAsync(buf, read, contentLength - read);
                    if (n == 0) break;
                    read += n;
                }
                var rawBytes = Encoding.Latin1.GetBytes(buf, 0, read);
                if (contentEncoding.Contains("gzip", StringComparison.OrdinalIgnoreCase)
                    || (rawBytes.Length >= 2 && rawBytes[0] == 0x1F && rawBytes[1] == 0x8B))
                {
                    using var compressed = new MemoryStream(rawBytes);
                    using var gzip = new System.IO.Compression.GZipStream(compressed, System.IO.Compression.CompressionMode.Decompress);
                    using var decompressed = new MemoryStream();
                    await gzip.CopyToAsync(decompressed);
                    body = Encoding.UTF8.GetString(decompressed.ToArray());
                }
                else
                {
                    body = Encoding.UTF8.GetString(rawBytes);
                }
            }

            lock (Requests)
                Requests.Add((method, path, body));

            var (respBody, status) = Router(method, path);
            var respBytes = Encoding.UTF8.GetBytes(respBody);
            var header = new StringBuilder()
                .Append($"HTTP/1.1 {status} {(status == 200 ? "OK" : "Error")}\r\n")
                .Append("Content-Type: application/json\r\n")
                .Append("X-Elastic-Product: Elasticsearch\r\n")
                .Append($"Content-Length: {respBytes.Length}\r\n")
                .Append("Connection: close\r\n\r\n")
                .ToString();
            var headerBytes = Encoding.ASCII.GetBytes(header);
            await stream.WriteAsync(headerBytes);
            await stream.WriteAsync(respBytes);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _listener.Stop();
        try { await _loop; } catch (OperationCanceledException) { }
        _cts.Dispose();
    }

    public static class Bodies
    {
        public const string BulkOk = """{"errors":false,"took":1,"items":[]}""";

        public const string Error500 =
            """{"error":{"type":"test_exception","reason":"boom"},"status":500}""";

        public static string IndexCreated(string id) =>
            "{\"_index\":\"products_v1\",\"_id\":\"" + id + "\",\"_version\":1,\"result\":\"created\",\"_shards\":{\"total\":1,\"successful\":1,\"failed\":0},\"_seq_no\":1,\"_primary_term\":1}";

        public static string Deleted(string id) =>
            "{\"_index\":\"products_v1\",\"_id\":\"" + id + "\",\"_version\":2,\"result\":\"deleted\",\"_shards\":{\"total\":1,\"successful\":1,\"failed\":0},\"_seq_no\":2,\"_primary_term\":1}";

        public static string SearchHits(string hitsArrayJson, long total) =>
            "{\"took\":1,\"timed_out\":false,\"_shards\":{\"total\":1,\"successful\":1,\"skipped\":0,\"failed\":0},\"hits\":{\"total\":{\"value\":" + total + ",\"relation\":\"eq\"},\"max_score\":1.0,\"hits\":" + hitsArrayJson + "}}";

        public static string SearchHit(string id, string sourceJson) =>
            "{\"_index\":\"products_v1\",\"_id\":\"" + id + "\",\"_score\":1.0,\"_source\":" + sourceJson + "}";

        public static string IndicesStats(long productsCount) =>
            "{\"_shards\":{\"total\":1,\"successful\":1,\"failed\":0},\"indices\":{\"products_v1\":{\"primaries\":{},\"total\":{\"docs\":{\"count\":" + productsCount + ",\"deleted\":0},\"store\":{\"size_in_bytes\":100}}}}}";

        public const string ClusterHealthGreen =
            """{"cluster_name":"test-cluster","status":"green","timed_out":false,"number_of_nodes":2,"number_of_data_nodes":2,"active_primary_shards":10,"active_shards":10,"relocating_shards":0,"initializing_shards":0,"unassigned_shards":0,"delayed_unassigned_shards":0,"number_of_pending_tasks":0,"number_of_in_flight_fetch":0,"task_max_waiting_in_queue_millis":0,"active_shards_percent_as_number":100.0}""";
    }
}
