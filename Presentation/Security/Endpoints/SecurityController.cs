using MapsterMapper;

namespace Presentation.Security.Endpoints;

[Route("api/[controller]")]
[ApiController]
public class SecurityController(
    IAntiforgery antiforgery,
    IMediator mediator,
    IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet("csrf-token")]
    [IgnoreAntiforgeryToken]
    public IActionResult GetCsrfToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }
}