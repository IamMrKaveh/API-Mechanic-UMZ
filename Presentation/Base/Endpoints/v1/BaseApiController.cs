using Application.Common.Interfaces;
using Application.Common.Results;
using SharedKernel.Results;

namespace Presentation.Base.Endpoints.v1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseApiController(ISender mediator, IMapper? mapper = null) : ControllerBase
{
    protected readonly ISender Mediator = mediator;
    protected readonly IMapper? Mapper = mapper;

    private ICurrentUserService? _requestContext;

    protected ICurrentUserService RequestContext =>
        _requestContext ??= HttpContext.RequestServices.GetRequiredService<ICurrentUserService>();

    protected async Task<IActionResult> Send<TResponse>(
        IRequest<ServiceResult<TResponse>> request,
        CancellationToken ct)
    {
        return ToActionResult(await Mediator.Send(request, ct));
    }

    protected async Task<IActionResult> Send(
        IRequest<ServiceResult> request,
        CancellationToken ct)
    {
        return ToActionResult(await Mediator.Send(request, ct));
    }

    protected IActionResult ToActionResult<T>(ServiceResult<T> result)
    {
        if (result.IsSuccess)
            return Ok(new ApiResponse<T>(result.Value, true, null));

        var statusCode = MapStatusCode(result.Type);
        var errors = result.Error is null
            ? null
            : new Dictionary<string, string[]> { ["domain"] = [result.Error] };

        return StatusCode(statusCode, new ApiResponse<T>(default, false, result.Error, errors));
    }

    protected IActionResult ToActionResult(ServiceResult result)
    {
        if (result.IsSuccess)
            return Ok(new ApiResponse(true, null));

        var statusCode = MapStatusCode(result.Type);
        var errors = result.Error is null
            ? null
            : new Dictionary<string, string[]> { ["domain"] = [result.Error] };

        return StatusCode(statusCode, new ApiResponse(false, result.Error, errors));
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