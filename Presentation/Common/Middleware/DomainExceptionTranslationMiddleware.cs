using Presentation.Localization;

namespace Presentation.Common.Middleware;

public sealed class DomainExceptionTranslationMiddleware
{
    private readonly RequestDelegate _next;

    public DomainExceptionTranslationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            var translated = WalletErrorTranslator.Translate(ex);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json; charset=utf-8";

            var payload = new
            {
                success = false,
                errorCode = ex.ErrorCode,
                message = translated,
                args = ex.Args
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
