using Application.Common.Results;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using SharedKernel.Results;

namespace Presentation.Common.Mappers;

public sealed class HttpResultMapper : IHttpResultMapper
{
    public IActionResult Map<T>(ServiceResult<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(new ApiResponse<T>(result.Value, true, null));

        var statusCode = MapStatusCode(result.Type);
        var errors = BuildErrors(result.Error);

        return new ObjectResult(new ApiResponse<T>(default, false, result.Error, errors))
        {
            StatusCode = statusCode
        };
    }

    public IActionResult Map(ServiceResult result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(new ApiResponse(true, null));

        var statusCode = MapStatusCode(result.Type);
        var errors = BuildErrors(result.Error);

        return new ObjectResult(new ApiResponse(false, result.Error, errors))
        {
            StatusCode = statusCode
        };
    }

    private static Dictionary<string, string[]> BuildErrors(string? error)
    {
        if (error is null) return [];
        return new Dictionary<string, string[]> { ["domain"] = [error] };
    }

    private static int MapStatusCode(ErrorType type) =>
        type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.RateLimitExceeded => StatusCodes.Status429TooManyRequests,
            _ => StatusCodes.Status500InternalServerError
        };
}