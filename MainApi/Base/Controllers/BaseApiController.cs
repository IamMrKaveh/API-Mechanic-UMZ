namespace MainApi.Base.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController(ISender mediator) : ControllerBase
{
    protected readonly ISender Mediator = mediator;

    protected CurrentUser CurrentUser =>
        HttpContext.RequestServices.GetRequiredService<ICurrentUserService>().CurrentUser;

    protected IActionResult ToActionResult<T>(ServiceResult<T> result) =>
        result.Status switch
        {
            ServiceResultStatus.Success => Ok(result.Value),
            ServiceResultStatus.Created => StatusCode(201, result.Value),
            ServiceResultStatus.NoContent => NoContent(),
            ServiceResultStatus.NotFound => NotFound(result.Error),
            ServiceResultStatus.BadRequest => BadRequest(result.Error),
            ServiceResultStatus.Unauthorized => Unauthorized(result.Error),
            ServiceResultStatus.Forbidden => Forbid(),
            ServiceResultStatus.Conflict => Conflict(result.Error),
            ServiceResultStatus.UnprocessableEntity => UnprocessableEntity(result.Error),
            _ => StatusCode(500, result.Error)
        };

    protected IActionResult ToCreatedResult<T>(ServiceResult<T> result)
    {
        if (!result.IsSuccess)
            return ToActionResult(result);
        return StatusCode(201, result.Value);
    }
}