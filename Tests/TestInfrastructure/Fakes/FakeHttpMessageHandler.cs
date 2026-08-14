namespace Tests.TestInfrastructure.Fakes;

public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _responder;

    public List<HttpRequestMessage> Requests { get; } = new();

    public List<string> RequestBodies { get; } = new();

    public int CallCount => Requests.Count;

    public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
    {
        _responder = responder;
    }

    public static FakeHttpMessageHandler WithResponse(HttpStatusCode statusCode, string body = "", string mediaType = "application/json")
    {
        return new FakeHttpMessageHandler((_, _) =>
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, mediaType)
            };
            return Task.FromResult(response);
        });
    }

    public static FakeHttpMessageHandler WithSequence(params Func<HttpStatusCode, string>[] _)
    {
        throw new NotSupportedException();
    }

    public static FakeHttpMessageHandler ThrowsException(Exception exception)
    {
        return new FakeHttpMessageHandler((_, _) => throw exception);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        Requests.Add(request);

        if (request.Content is not null)
        {
            var body = await request.Content.ReadAsStringAsync(cancellationToken);
            RequestBodies.Add(body);
        }
        else
        {
            RequestBodies.Add(string.Empty);
        }

        return await _responder(request, cancellationToken);
    }
}
