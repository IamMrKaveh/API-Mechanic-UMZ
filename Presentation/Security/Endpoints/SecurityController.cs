namespace Presentation.Security.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/security")]
public class SecurityController(
    IAntiforgery antiforgery,
    IMediator mediator,
    IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet("csrf-token")]
    [IgnoreAntiforgeryToken]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetCsrfToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }
}