using Application.Audit.Contracts;
using Application.Common.Exceptions;
using Presentation.Common.ProblemDetails;
using ValidationException = FluentValidation.ValidationException;

namespace Presentation.Common.Middleware;

public class CustomExceptionHandlerMiddleware(
    RequestDelegate next,
    IServiceScopeFactory scopeFactory,
    ILogger<CustomExceptionHandlerMiddleware> logger)
{
    private const string ProblemJsonContentType = "application/problem+json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
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
        var (statusCode, problem, isUnhandled) = MapException(context, exception);

        LogException(context, exception, (int)statusCode, isUnhandled);

        if (isUnhandled)
            await LogAuditAsync(exception);

        if (context.Response.HasStarted)
        {
            logger.LogWarning(
                "Cannot write ProblemDetails response for {Path} because response has already started.",
                context.Request.Path);
            return;
        }

        context.Response.Clear();
        context.Response.ContentType = ProblemJsonContentType;
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, SerializerOptions));
    }

    private static (HttpStatusCode StatusCode, PersianProblemDetails Body, bool IsUnhandled) MapException(
        HttpContext context,
        Exception exception)
    {
        var instance = context.Request.Path.Value;
        var traceId = context.TraceIdentifier;

        return exception switch
        {
            ValidationException ve => (
                HttpStatusCode.BadRequest,
                PersianProblemDetailsFactory.FromValidation(ve.Errors, instance, traceId),
                false),

            DomainException de => (
                HttpStatusCode.BadRequest,
                PersianProblemDetailsFactory.FromDomainException(de, HttpStatusCode.BadRequest, instance, traceId),
                false),

            KeyNotFoundException knf => (
                HttpStatusCode.NotFound,
                PersianProblemDetailsFactory.FromStatus(HttpStatusCode.NotFound, knf.Message, instance, traceId, "NOT_FOUND"),
                false),

            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                PersianProblemDetailsFactory.FromStatus(HttpStatusCode.Unauthorized, null, instance, traceId, "UNAUTHORIZED"),
                false),

            ConcurrencyException ce => (
                HttpStatusCode.Conflict,
                PersianProblemDetailsFactory.FromStatus(HttpStatusCode.Conflict, ce.Message, instance, traceId, "CONCURRENCY_CONFLICT"),
                false),

            DbUpdateConcurrencyException => (
                HttpStatusCode.Conflict,
                PersianProblemDetailsFactory.FromStatus(
                    HttpStatusCode.Conflict,
                    "تغییرات همزمان رخ داده است. لطفاً دوباره تلاش کنید.",
                    instance,
                    traceId,
                    "CONCURRENCY_CONFLICT"),
                false),

            DbUpdateException dbEx when IsPgUniqueViolation(dbEx) => (
                HttpStatusCode.Conflict,
                PersianProblemDetailsFactory.FromStatus(
                    HttpStatusCode.Conflict,
                    "داده تکراری است.",
                    instance,
                    traceId,
                    "DUPLICATE_DATA"),
                false),

            OperationCanceledException => (
                (HttpStatusCode)499,
                PersianProblemDetailsFactory.FromStatus((HttpStatusCode)499, null, instance, traceId, "CLIENT_CLOSED_REQUEST"),
                false),

            _ => (
                HttpStatusCode.InternalServerError,
                PersianProblemDetailsFactory.FromStatus(
                    HttpStatusCode.InternalServerError,
                    null,
                    instance,
                    traceId,
                    "INTERNAL_SERVER_ERROR"),
                true)
        };
    }

    private static bool IsPgUniqueViolation(DbUpdateException ex)
        => ex.InnerException is Npgsql.PostgresException pg && pg.SqlState == "23505";

    private void LogException(HttpContext context, Exception exception, int statusCode, bool isUnhandled)
    {
        var level = isUnhandled ? LogLevel.Error : LogLevel.Warning;

        logger.Log(level, exception,
            "Request {Method} {Path} failed with {StatusCode} ({ExceptionType}): {Message} | TraceId={TraceId}",
            context.Request.Method,
            context.Request.Path,
            statusCode,
            exception.GetType().Name,
            exception.Message,
            context.TraceIdentifier);
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
