using Presentation.Common.Interfaces;

namespace Presentation.Common.Mappers;

public sealed class HttpResultMapper : IHttpResultMapper
{
    public IActionResult Map(ServiceResult result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(new ApiResponse(true, null));

        var statusCode = MapStatusCode(result.Error.Type);
        var errors = BuildErrors(result.Error);

        return new ObjectResult(new ApiResponse(false, result.Error.Message, errors))
        {
            StatusCode = statusCode
        };
    }

    public IActionResult Map<T>(ServiceResult<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(new ApiResponse<T>(result.Value, true, null));

        var statusCode = MapStatusCode(result.Error.Type);
        var errors = BuildErrors(result.Error);

        return new ObjectResult(new ApiResponse<T>(default, false, result.Error.Message, errors))
        {
            StatusCode = statusCode
        };
    }

    public IActionResult MapCreated<T>(ServiceResult<T> result, string? location = null)
    {
        if (result.IsFailure)
            return Map(result);

        var body = new ApiResponse<T>(result.Value, true, null);
        return string.IsNullOrWhiteSpace(location)
            ? new ObjectResult(body) { StatusCode = StatusCodes.Status201Created }
            : new CreatedResult(location, body);
    }

    private static int MapStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.RateLimitExceeded => StatusCodes.Status429TooManyRequests,
        ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError
    };

    private static Dictionary<string, string[]> BuildErrors(Error error)
    {
        if (error.ValidationErrors is { Count: > 0 } list)
        {
            return list
                .GroupBy(v => string.IsNullOrWhiteSpace(v.Property) ? "domain" : v.Property)
                .ToDictionary(g => g.Key, g => g.Select(v => v.Message).ToArray());
        }

        if (string.IsNullOrWhiteSpace(error.Message))
            return new Dictionary<string, string[]>();

        return new Dictionary<string, string[]>
        {
            [string.IsNullOrWhiteSpace(error.Code) ? "domain" : error.Code] = new[] { error.Message }
        };
    }
}