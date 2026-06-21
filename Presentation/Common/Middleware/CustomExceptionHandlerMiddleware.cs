using Application.Audit.Contracts;
using Application.Common.Exceptions;
using SharedKernel.Exceptions;

namespace Presentation.Common.Middleware;

public class CustomExceptionHandlerMiddleware(
    RequestDelegate next,
    IServiceScopeFactory scopeFactory,
    ILogger<CustomExceptionHandlerMiddleware> logger)
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
        var (statusCode, body, isUnhandled) = MapException(exception);

        LogException(context, exception, (int)statusCode, isUnhandled);

        if (isUnhandled)
            await LogAuditAsync(exception);

        if (context.Response.HasStarted)
            return;

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(body, SerializerOptions));
    }

    private static (HttpStatusCode StatusCode, ApiResponse Body, bool IsUnhandled) MapException(Exception exception)
        => exception switch
        {
            ValidationException ve => (HttpStatusCode.BadRequest, BuildValidation(ve), false),
            DomainException de => (HttpStatusCode.BadRequest, new ApiResponse(false, de.Message), false),
            KeyNotFoundException => (HttpStatusCode.NotFound, new ApiResponse(false, exception.Message), false),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, new ApiResponse(false, "دسترسی غیرمجاز."), false),
            ConcurrencyException ce => (HttpStatusCode.Conflict, new ApiResponse(false, ce.Message), false),
            DbUpdateConcurrencyException => (HttpStatusCode.Conflict, new ApiResponse(false, "تغییرات همزمان رخ داده است. لطفاً دوباره تلاش کنید."), false),
            DbUpdateException dbEx when IsPgUniqueViolation(dbEx) => (HttpStatusCode.Conflict, new ApiResponse(false, "داده تکراری است."), false),
            OperationCanceledException => ((HttpStatusCode)499, new ApiResponse(false, "درخواست لغو شد."), false),
            _ => (HttpStatusCode.InternalServerError, new ApiResponse(false, "خطای غیرمنتظره‌ای رخ داده است."), true)
        };

    private static bool IsPgUniqueViolation(DbUpdateException ex)
    => ex.InnerException is Npgsql.PostgresException pg && pg.SqlState == "23505";

    private void LogException(HttpContext context, Exception exception, int statusCode, bool isUnhandled)
    {
        var level = isUnhandled ? Microsoft.Extensions.Logging.LogLevel.Error : Microsoft.Extensions.Logging.LogLevel.Warning;
        logger.Log(level, exception,
            "Request {Method} {Path} failed with {StatusCode}: {Message}",
            context.Request.Method,
            context.Request.Path,
            statusCode,
            exception.Message);
    }

    private static ApiResponse BuildValidation(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return new ApiResponse(false, "اطلاعات ورودی نامعتبر است.", errors);
    }

    private async Task LogAuditAsync(Exception exception)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();
            await auditService.LogErrorAsync(
                $"Unhandled exception: {exception.GetType().Name} — {exception.Message}\n{exception.StackTrace}");
        }
        catch (Exception auditEx)
        {
            logger.LogError(auditEx, "Failed to write unhandled exception to audit log.");
        }
    }
}

public static class CustomExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder builder)
        => builder.UseMiddleware<CustomExceptionHandlerMiddleware>();
}