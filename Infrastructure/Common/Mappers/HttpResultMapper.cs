using Application.Common.Interfaces;
using Application.Common.Results;
using Microsoft.AspNetCore.Mvc;
using ResultType = Application.Common.Results.ResultType;

namespace Infrastructure.Common.Mappers;

public sealed class HttpResultMapper : IHttpResultMapper
{
    public IActionResult Map<T>(ServiceResult<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        var statusCode = MapStatusCode(result.Type);

        var problem = new ProblemDetails
        {
            Title = result.Error?.Message ?? "Request failed",
            Detail = result.Error?.Message,
            Status = statusCode,
            Extensions =
            {
                ["code"] = result.Error?.Code,
                ["metadata"] = result.Error?.Metadata
            }
        };

        return new ObjectResult(problem)
        {
            StatusCode = statusCode
        };
    }

    private static int MapStatusCode(ResultType type) =>
        type switch
        {
            ResultType.ValidationError => StatusCodes.Status400BadRequest,
            ResultType.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultType.Forbidden => StatusCodes.Status403Forbidden,
            ResultType.NotFound => StatusCodes.Status404NotFound,
            ResultType.Conflict => StatusCodes.Status409Conflict,
            ResultType.Unexpected => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };
}