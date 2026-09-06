using Microsoft.AspNetCore.Http;
using Presentation.Common.Middleware;
using Presentation.Localization;
using SharedKernel.Exceptions;
using SharedKernel.Localization;

namespace Tests.Presentation.Common.Middleware;

public class DomainExceptionTranslationMiddlewareTests
{
    private static DefaultHttpContext BuildContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static string ReadBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return new StreamReader(context.Response.Body).ReadToEnd();
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_CallsNextAndLeavesResponseAlone()
    {
        var called = false;
        var sut = new DomainExceptionTranslationMiddleware(
            _ => { called = true; return Task.CompletedTask; });
        var context = BuildContext();

        await sut.InvokeAsync(context);

        called.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(200);
    }

    [Fact]
    public async Task InvokeAsync_WhenKnownWalletError_ReturnsTranslatedMessage()
    {
        var sut = new DomainExceptionTranslationMiddleware(_ =>
            Task.FromException(new DomainException(
                DomainErrorCodes.Wallet.TransferSelfNotAllowed,
                "raw message")));
        var context = BuildContext();

        await sut.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        context.Response.ContentType.ShouldBe("application/json; charset=utf-8");
        using var doc = JsonDocument.Parse(ReadBody(context));
        doc.RootElement.GetProperty("success").GetBoolean().ShouldBeFalse();
        doc.RootElement.GetProperty("errorCode").GetString()
            .ShouldBe(DomainErrorCodes.Wallet.TransferSelfNotAllowed);
        doc.RootElement.GetProperty("message").GetString()
            .ShouldBe(WalletErrorTranslator.Translate(new DomainException(
                DomainErrorCodes.Wallet.TransferSelfNotAllowed, "raw message")));
        doc.RootElement.GetProperty("message").GetString().ShouldNotBe("raw message");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnknownErrorCode_ReturnsOriginalMessage()
    {
        var sut = new DomainExceptionTranslationMiddleware(_ =>
            Task.FromException(new DomainException("SOME_UNKNOWN_CODE", "original text")));
        var context = BuildContext();

        await sut.InvokeAsync(context);

        context.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        using var doc = JsonDocument.Parse(ReadBody(context));
        doc.RootElement.GetProperty("errorCode").GetString().ShouldBe("SOME_UNKNOWN_CODE");
        doc.RootElement.GetProperty("message").GetString().ShouldBe("original text");
    }

    [Fact]
    public async Task InvokeAsync_IncludesExceptionArgs()
    {
        var args = new Dictionary<string, object?> { ["minimum"] = 5000 };
        var sut = new DomainExceptionTranslationMiddleware(_ =>
            Task.FromException(new DomainException(
                DomainErrorCodes.Wallet.TransferMinimumAmount, "raw", args)));
        var context = BuildContext();

        await sut.InvokeAsync(context);

        using var doc = JsonDocument.Parse(ReadBody(context));
        doc.RootElement.GetProperty("args").GetProperty("minimum").GetInt32().ShouldBe(5000);
        doc.RootElement.GetProperty("message").GetString().ShouldContain("تومان");
    }

    [Fact]
    public async Task InvokeAsync_WhenNonDomainException_Rethrows()
    {
        var failure = new InvalidOperationException("unexpected");
        var sut = new DomainExceptionTranslationMiddleware(_ => Task.FromException(failure));

        var thrown = await Should.ThrowAsync<InvalidOperationException>(() =>
            sut.InvokeAsync(BuildContext()));

        thrown.ShouldBeSameAs(failure);
    }
}
