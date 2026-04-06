using Presentation.Base.Controllers.v1;

namespace Presentation.Security.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SecurityController(IAntiforgery antiforgery, IMediator mediator) : BaseApiController(mediator)
{
    private readonly IAntiforgery _antiforgery = antiforgery;

    [HttpGet("csrf-token")]
    [IgnoreAntiforgeryToken]
    public IActionResult GetCsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        return Ok(new { token = tokens.RequestToken });
    }
}