namespace MainApi.Middleware;

public class CustomExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CustomExceptionHandlerMiddleware> _logger;

    public CustomExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<CustomExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, body) = exception switch
        {
            ValidationException ve => MapValidationException(ve),
            Domain.Common.Exceptions.DomainException de => MapDomainException(de),
            KeyNotFoundException => MapNotFoundException(exception),
            UnauthorizedAccessException => MapUnauthorizedException(),
            _ => MapUnhandledException(exception)
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        return context.Response.WriteAsync(JsonSerializer.Serialize(body));
    }

    private static (HttpStatusCode, object) MapValidationException(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return (HttpStatusCode.BadRequest, new
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Message = "Validation Failed",
            Errors = errors
        });
    }

    private static (HttpStatusCode, object) MapDomainException(
        Domain.Common.Exceptions.DomainException exception)
        => (HttpStatusCode.BadRequest, new
        {
            StatusCode = (int)HttpStatusCode.BadRequest,
            Message = exception.Message
        });

    private static (HttpStatusCode, object) MapNotFoundException(Exception exception)
        => (HttpStatusCode.NotFound, new
        {
            StatusCode = (int)HttpStatusCode.NotFound,
            Message = exception.Message
        });

    private static (HttpStatusCode, object) MapUnauthorizedException()
        => (HttpStatusCode.Unauthorized, new
        {
            StatusCode = (int)HttpStatusCode.Unauthorized,
            Message = "Unauthorized"
        });

    private (HttpStatusCode, object) MapUnhandledException(Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception has occurred.");

        return (HttpStatusCode.InternalServerError, new
        {
            StatusCode = (int)HttpStatusCode.InternalServerError,
            Message = "An unexpected error occurred."
        });
    }
}

public static class CustomExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder builder)
        => builder.UseMiddleware<CustomExceptionHandlerMiddleware>();
}