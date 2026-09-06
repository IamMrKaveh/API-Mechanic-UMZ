using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Presentation.Common.Middleware;

namespace Tests.Presentation.Common.Middleware;

public class RequestLoggingMiddlewareTests
{
    private readonly ILogger<RequestLoggingMiddleware> _logger =
        Substitute.For<ILogger<RequestLoggingMiddleware>>();

    private static DefaultHttpContext BuildContext(int statusCode = 200)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/orders";
        context.Response.StatusCode = statusCode;
        return context;
    }

    [Fact]
    public async Task InvokeAsync_SuccessfulRequest_CompletesAndCallsNext()
    {
        var called = false;
        var sut = new RequestLoggingMiddleware(
            _ => { called = true; return Task.CompletedTask; }, _logger);

        await sut.InvokeAsync(BuildContext());

        called.ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenLoggerEnabled_LogsRequest()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var sut = new RequestLoggingMiddleware(_ => Task.CompletedTask, _logger);

        await sut.InvokeAsync(BuildContext(200));

        _logger.ReceivedWithAnyArgs(1).Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task InvokeAsync_WhenLoggerDisabled_DoesNotLog()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(false);
        var sut = new RequestLoggingMiddleware(_ => Task.CompletedTask, _logger);

        await sut.InvokeAsync(BuildContext(200));

        _logger.DidNotReceiveWithAnyArgs().Log(
            Arg.Any<LogLevel>(),
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task InvokeAsync_WhenNextThrows_RethrowsAfterLogging()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        var failure = new InvalidOperationException("downstream failed");
        var sut = new RequestLoggingMiddleware(_ => Task.FromException(failure), _logger);

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() =>
            sut.InvokeAsync(BuildContext()));

        thrown.ShouldBeSameAs(failure);
    }

    [Fact]
    public async Task InvokeAsync_ServerError_LogsAtErrorLevel()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        LogLevel? capturedLevel = null;
        _logger.When(x => x.Log(
                Arg.Any<LogLevel>(), Arg.Any<EventId>(), Arg.Any<object>(),
                Arg.Any<Exception?>(), Arg.Any<Func<object, Exception?, string>>()))
            .Do(call => capturedLevel = call.Arg<LogLevel>());
        var sut = new RequestLoggingMiddleware(_ => Task.CompletedTask, _logger);

        await sut.InvokeAsync(BuildContext(500));

        capturedLevel.ShouldBe(LogLevel.Error);
    }

    [Fact]
    public async Task InvokeAsync_ClientError_LogsAtWarningLevel()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        LogLevel? capturedLevel = null;
        _logger.When(x => x.Log(
                Arg.Any<LogLevel>(), Arg.Any<EventId>(), Arg.Any<object>(),
                Arg.Any<Exception?>(), Arg.Any<Func<object, Exception?, string>>()))
            .Do(call => capturedLevel = call.Arg<LogLevel>());
        var sut = new RequestLoggingMiddleware(_ => Task.CompletedTask, _logger);

        await sut.InvokeAsync(BuildContext(404));

        capturedLevel.ShouldBe(LogLevel.Warning);
    }
}
