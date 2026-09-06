using Application.Audit.Contracts;
using Application.Common.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Presentation.Common.Middleware;
using SharedKernel.Exceptions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Tests.Presentation.Common.Middleware;

public class CustomExceptionHandlerMiddlewareTests
{
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly ILogger<CustomExceptionHandlerMiddleware> _logger =
        Substitute.For<ILogger<CustomExceptionHandlerMiddleware>>();

    private CustomExceptionHandlerMiddleware BuildSut(RequestDelegate next)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_audit);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().Returns(_ => services.BuildServiceProvider().CreateScope());
        return new CustomExceptionHandlerMiddleware(next, scopeFactory, _logger);
    }

    private static DefaultHttpContext BuildContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/orders";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static JsonDocument ReadProblem(DefaultHttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return JsonDocument.Parse(new StreamReader(context.Response.Body).ReadToEnd());
    }

    [Fact]
    public async Task Invoke_WhenNoException_CallsNextAndWritesNothing()
    {
        var called = false;
        var sut = BuildSut(_ => { called = true; return Task.CompletedTask; });
        var context = BuildContext();

        await sut.Invoke(context);

        called.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task Invoke_FluentValidationException_Returns400ValidationProblem()
    {
        var ex = new FluentValidation.ValidationException(new List<FluentValidation.Results.ValidationFailure>
        {
            new("Name", "نام الزامی است.")
        });
        var sut = BuildSut(_ => Task.FromException(ex));
        var context = BuildContext();

        await sut.Invoke(context);

        context.Response.StatusCode.ShouldBe(400);
        context.Response.ContentType.ShouldBe("application/problem+json");
        using var doc = ReadProblem(context);
        doc.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        doc.RootElement.GetProperty("errorCode").GetString().ShouldBe("VALIDATION_ERROR");
        doc.RootElement.GetProperty("errors").GetProperty("Name")[0].GetString().ShouldBe("نام الزامی است.");
        await _audit.DidNotReceiveWithAnyArgs().LogErrorAsync(default!, default);
    }

    [Fact]
    public async Task Invoke_DomainException_Returns400WithErrorCode()
    {
        var sut = BuildSut(_ => Task.FromException(new DomainException("WALLET_X", "خطای دامنه.")));
        var context = BuildContext();

        await sut.Invoke(context);

        context.Response.StatusCode.ShouldBe(400);
        using var doc = ReadProblem(context);
        doc.RootElement.GetProperty("detail").GetString().ShouldBe("خطای دامنه.");
        doc.RootElement.GetProperty("errorCode").GetString().ShouldBe("WALLET_X");
    }

    [Fact]
    public async Task Invoke_KeyNotFoundException_Returns404()
    {
        var sut = BuildSut(_ => Task.FromException(new KeyNotFoundException("رکورد یافت نشد.")));
        var context = BuildContext();

        await sut.Invoke(context);

        context.Response.StatusCode.ShouldBe(404);
        using var doc = ReadProblem(context);
        doc.RootElement.GetProperty("errorCode").GetString().ShouldBe("NOT_FOUND");
        doc.RootElement.GetProperty("detail").GetString().ShouldBe("رکورد یافت نشد.");
    }

    [Fact]
    public async Task Invoke_UnauthorizedAccessException_Returns401()
    {
        var sut = BuildSut(_ => Task.FromException(new UnauthorizedAccessException("nope")));
        var context = BuildContext();

        await sut.Invoke(context);

        context.Response.StatusCode.ShouldBe(401);
        using var doc = ReadProblem(context);
        doc.RootElement.GetProperty("errorCode").GetString().ShouldBe("UNAUTHORIZED");
    }

    [Fact]
    public async Task Invoke_ConcurrencyException_Returns409()
    {
        var sut = BuildSut(_ => Task.FromException(new ConcurrencyException("تعارض نسخه.")));
        var context = BuildContext();

        await sut.Invoke(context);

        context.Response.StatusCode.ShouldBe(409);
        using var doc = ReadProblem(context);
        doc.RootElement.GetProperty("errorCode").GetString().ShouldBe("CONCURRENCY_CONFLICT");
        doc.RootElement.GetProperty("detail").GetString().ShouldBe("تعارض نسخه.");
    }

    [Fact]
    public async Task Invoke_DbUpdateConcurrencyException_Returns409()
    {
        var sut = BuildSut(_ => Task.FromException(new DbUpdateConcurrencyException()));
        var context = BuildContext();

        await sut.Invoke(context);

        context.Response.StatusCode.ShouldBe(409);
        using var doc = ReadProblem(context);
        doc.RootElement.GetProperty("errorCode").GetString().ShouldBe("CONCURRENCY_CONFLICT");
    }

    [Fact]
    public async Task Invoke_DbUpdateExceptionWithUniqueViolation_Returns409Duplicate()
    {
        var pg = (PostgresException)RuntimeHelpers.GetUninitializedObject(typeof(PostgresException));
        typeof(PostgresException)
            .GetField("<SqlState>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(pg, "23505");
        var sut = BuildSut(_ => Task.FromException(new DbUpdateException("dup", pg)));
        var context = BuildContext();

        await sut.Invoke(context);

        context.Response.StatusCode.ShouldBe(409);
        using var doc = ReadProblem(context);
        doc.RootElement.GetProperty("errorCode").GetString().ShouldBe("DUPLICATE_DATA");
        doc.RootElement.GetProperty("detail").GetString().ShouldBe("داده تکراری است.");
    }

    [Fact]
    public async Task Invoke_DbUpdateExceptionWithoutUniqueViolation_Returns500AndAudits()
    {
        var sut = BuildSut(_ => Task.FromException(new DbUpdateException("db down")));
        var context = BuildContext();

        await sut.Invoke(context);

        context.Response.StatusCode.ShouldBe(500);
        using var doc = ReadProblem(context);
        doc.RootElement.GetProperty("errorCode").GetString().ShouldBe("INTERNAL_SERVER_ERROR");
        await _audit.Received(1).LogErrorAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invoke_OperationCanceledException_Returns499()
    {
        var sut = BuildSut(_ => Task.FromException(new OperationCanceledException()));
        var context = BuildContext();

        await sut.Invoke(context);

        context.Response.StatusCode.ShouldBe(499);
        using var doc = ReadProblem(context);
        doc.RootElement.GetProperty("errorCode").GetString().ShouldBe("CLIENT_CLOSED_REQUEST");
    }

    [Fact]
    public async Task Invoke_UnhandledException_Returns500AndAudits()
    {
        var sut = BuildSut(_ => Task.FromException(new InvalidOperationException("boom")));
        var context = BuildContext();

        await sut.Invoke(context);

        context.Response.StatusCode.ShouldBe(500);
        context.Response.ContentType.ShouldBe("application/problem+json");
        using var doc = ReadProblem(context);
        doc.RootElement.GetProperty("status").GetInt32().ShouldBe(500);
        doc.RootElement.GetProperty("traceId").GetString().ShouldBe(context.TraceIdentifier);
        doc.RootElement.GetProperty("instance").GetString().ShouldBe("/api/orders");
        await _audit.Received(1).LogErrorAsync(
            Arg.Is<string>(s => s.Contains(nameof(InvalidOperationException))),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void UseCustomExceptionHandler_ReturnsBuilder()
    {
        var app = new ApplicationBuilder(
            new ServiceCollection().BuildServiceProvider());

        app.UseCustomExceptionHandler().ShouldBeSameAs(app);
    }
}
