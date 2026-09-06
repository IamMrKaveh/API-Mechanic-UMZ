using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Common.Extensions;

namespace Tests.Presentation.Common.Extensions;

public class SecurityExtensionsTests
{
    private static (ApplicationBuilder App, RequestDelegate Pipeline) BuildPipeline(
        RequestDelegate terminal)
    {
        var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());
        app.UseRequestPerformanceMonitoring();
        app.Run(terminal);
        return (app, app.Build());
    }

    [Fact]
    public void UseRequestPerformanceMonitoring_ReturnsSameBuilder()
    {
        var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());

        app.UseRequestPerformanceMonitoring().ShouldBeSameAs(app);
    }

    [Fact]
    public async Task Middleware_PassesSuccessfulRequestThrough()
    {
        var (_, pipeline) = BuildPipeline(context =>
        {
            context.Response.StatusCode = 200;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();

        await pipeline(context);

        context.Response.StatusCode.ShouldBe(200);
    }

    private static bool ShouldLog(HttpContext context, long elapsedMs)
    {
        var method = typeof(SecurityExtensions).GetMethod(
            "ShouldLogRequest", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (bool)method.Invoke(null, [context, elapsedMs])!;
    }

    private static DefaultHttpContext ContextWithStatus(int status)
    {
        var context = new DefaultHttpContext();
        context.Response.StatusCode = status;
        return context;
    }

    [Theory]
    [InlineData(200, 100, false)]
    [InlineData(200, 2500, true)]
    [InlineData(500, 10, true)]
    [InlineData(429, 10, true)]
    [InlineData(401, 10, false)]
    [InlineData(403, 10, false)]
    [InlineData(404, 10, false)]
    [InlineData(404, 3000, true)]
    public void ShouldLogRequest_CoversSlowAndStatusBranches(int status, long elapsedMs, bool expected)
    {
        ShouldLog(ContextWithStatus(status), elapsedMs).ShouldBe(expected);
    }
}
