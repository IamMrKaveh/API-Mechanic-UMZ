using Microsoft.AspNetCore.Http;
using Presentation.Common.Middleware;

namespace Tests.Presentation.Common.Middleware;

public class CorrelationIdMiddlewareTests
{
    private static DefaultHttpContext BuildContext(string? incomingId = null)
    {
        var context = new DefaultHttpContext();
        if (incomingId is not null)
            context.Request.Headers["X-Correlation-ID"] = incomingId;
        return context;
    }

    [Fact]
    public async Task InvokeAsync_WithIncomingHeader_ReusesIt()
    {
        var context = BuildContext("corr-123");
        var called = false;
        var sut = new CorrelationIdMiddleware(_ => { called = true; return Task.CompletedTask; });

        await sut.InvokeAsync(context);

        called.ShouldBeTrue();
        context.TraceIdentifier.ShouldBe("corr-123");
        context.Response.Headers["X-Correlation-ID"].ToString().ShouldBe("corr-123");
    }

    [Fact]
    public async Task InvokeAsync_WithoutHeader_GeneratesGuid()
    {
        var context = BuildContext();
        var sut = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await sut.InvokeAsync(context);

        Guid.TryParse(context.TraceIdentifier, out _).ShouldBeTrue();
        context.Response.Headers["X-Correlation-ID"].ToString().ShouldBe(context.TraceIdentifier);
    }

    [Fact]
    public async Task InvokeAsync_AlwaysCallsNext()
    {
        var context = BuildContext();
        var called = false;
        var sut = new CorrelationIdMiddleware(_ => { called = true; return Task.CompletedTask; });

        await sut.InvokeAsync(context);

        called.ShouldBeTrue();
    }
}
