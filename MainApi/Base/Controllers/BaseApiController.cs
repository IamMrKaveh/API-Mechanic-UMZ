namespace MainApi.Base.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected readonly ICurrentUserService CurrentUser;

    protected BaseApiController(ICurrentUserService currentUserService)
    {
        CurrentUser = currentUserService;
    }

    protected IActionResult ToActionResult(ServiceResult result)
    {
        if (result.IsFailed)
        {
            return NoContent();
        }

        return result.Error switch
        {
            "NotFound" => NotFound(),
            _ when result.Error != null && result.Error.Contains("Concurrency") => Conflict(new { message = result.Error }),
            _ => BadRequest(new { message = result.Error })
        };
    }

    protected IActionResult ToActionResult<T>(ServiceResult<T> result)
    {
        if (result.IsFailed)
        {
            return result.Error switch
            {
                "NotFound" => NotFound(new { message = result.Error }),
                _ when result.Error != null && result.Error.Contains("Concurrency") => Conflict(new { message = result.Error }),
                _ => BadRequest(new { message = result.Error })
            };
        }

        if (result.Data == null)
        {
            return NotFound();
        }

        return Ok(result.Data);
    }
}