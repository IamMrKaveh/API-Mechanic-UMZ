using Application.Audit.Contracts;
using SharedKernel.Exceptions;

namespace Presentation.Common.Middleware;

public class CustomExceptionHandlerMiddleware(
    RequestDelegate next,
    IServiceScopeFactory scopeFactory)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, body) = exception switch
        {
            ValidationException ve => MapValidationException(ve),
            DomainException de => MapDomainException(de),
            KeyNotFoundException => MapNotFoundException(exception),
            UnauthorizedAccessException => MapUnauthorizedException(),
            _ => MapUnhandledException(exception)
        };

        if (exception is not ValidationException
            and not DomainException
            and not KeyNotFoundException
            and not UnauthorizedAccessException)
        {
            await LogUnhandledExceptionAsync(exception);
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, SerializerOptions));
    }

    private async Task LogUnhandledExceptionAsync(Exception exception)
    {
        using var scope = scopeFactory.CreateScope();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
        await auditService.LogErrorAsync(
            $"Unhandled exception: {exception.GetType().Name} — {exception.Message}\n{exception.StackTrace}");
    }

    private static (HttpStatusCode, ApiResponse) MapValidationException(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return (HttpStatusCode.BadRequest, new ApiResponse(false, "اطلاعات ورودی نامعتبر است.", errors));
    }

    private static (HttpStatusCode, ApiResponse) MapDomainException(DomainException exception)
        => (HttpStatusCode.BadRequest, new ApiResponse(false, exception.Message));

    private static (HttpStatusCode, ApiResponse) MapNotFoundException(Exception exception)
        => (HttpStatusCode.NotFound, new ApiResponse(false, exception.Message));

    private static (HttpStatusCode, ApiResponse) MapUnauthorizedException()
        => (HttpStatusCode.Unauthorized, new ApiResponse(false, "دسترسی غیرمجاز."));

    private static (HttpStatusCode, ApiResponse) MapUnhandledException(Exception exception)
        => (HttpStatusCode.InternalServerError, new ApiResponse(false, "خطای غیرمنتظره‌ای رخ داده است."));
}

public static class CustomExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder builder)
        => builder.UseMiddleware<CustomExceptionHandlerMiddleware>();
}