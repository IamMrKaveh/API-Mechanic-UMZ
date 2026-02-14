namespace MainApi.Middleware;

public class CustomExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CustomExceptionHandlerMiddleware> _logger;

    public CustomExceptionHandlerMiddleware(RequestDelegate next, ILogger<CustomExceptionHandlerMiddleware> logger)
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
        var code = HttpStatusCode.InternalServerError;
        object errorResponse;

        switch (exception)
        {
            case ValidationException validationException:
                code = HttpStatusCode.BadRequest;
                var errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                errorResponse = new
                {
                    StatusCode = (int)code,
                    Message = "Validation Failed",
                    Errors = errors
                };
                break;

            case Domain.Common.Exceptions.DomainException domainException:
                code = HttpStatusCode.BadRequest;
                errorResponse = new
                {
                    StatusCode = (int)code,
                    Message = domainException.Message
                };
                break;

            case KeyNotFoundException:
                code = HttpStatusCode.NotFound;
                errorResponse = new
                {
                    StatusCode = (int)code,
                    Message = exception.Message
                };
                break;

            case UnauthorizedAccessException:
                code = HttpStatusCode.Unauthorized;
                errorResponse = new
                {
                    StatusCode = (int)code,
                    Message = "Unauthorized"
                };
                break;

            default:
                _logger.LogError(exception, "An unhandled exception has occurred.");
                code = HttpStatusCode.InternalServerError;
                // In production, do not expose stack trace
                errorResponse = new
                {
                    StatusCode = (int)code,
                    Message = "An unexpected error occurred."
                };
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;

        return context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
}

public static class CustomExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CustomExceptionHandlerMiddleware>();
    }
}