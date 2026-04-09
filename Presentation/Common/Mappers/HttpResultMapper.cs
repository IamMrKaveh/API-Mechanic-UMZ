using Application.Common.Interfaces;
using Application.Common.Results;
using Presentation.Base.Responses;
using SharedKernel.Results;

namespace Presentation.Common.Mappers;

public sealed class HttpResultMapper : IHttpResultMapper
{
    public IActionResult Map<T>(ServiceResult<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(new ApiResponse<T>(result.Value, true, "Operation completed successfully.", null));

        var statusCode = MapStatusCode(result.Type);

        var problem = new ProblemDetails
        {
            Title = result.Error ?? "Request failed",
            Detail = result.Error,
            Status = statusCode
        };

        var errors = new Dictionary<string, string[]>();
        if (result.Error is not null)
        {
            errors.Add("Domain", [result.Error]);
        }

        var response = new ApiResponse<T>(default, false, problem.Title, errors);

        return new ObjectResult(response)
        {
            StatusCode = statusCode
        };
    }

    public IActionResult Map(ServiceResult result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(new ApiResponse(true, "Operation completed successfully.", null));

        var statusCode = MapStatusCode(result.Type);

        var errors = new Dictionary<string, string[]>();
        if (result.Error is not null)
        {
            errors.Add("Domain", [result.Error]);
        }

        var response = new ApiResponse(false, result.Error ?? "Request failed", errors);

        return new ObjectResult(response)
        {
            StatusCode = statusCode
        };
    }

    private static int MapStatusCode(ErrorType type) =>
        type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Failure => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
}