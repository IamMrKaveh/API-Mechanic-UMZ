using Microsoft.AspNetCore.Antiforgery;

namespace Presentation.Auth.Endpoints;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/csrf")]
[AllowAnonymous]
public sealed class CsrfController(IAntiforgery antiforgery) : ControllerBase
{
    private const string CookieName = "XSRF-TOKEN";

    [HttpGet("token")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult GetToken()
    {
        var tokens = antiforgery.GetAndStoreTokens(HttpContext);

        if (!string.IsNullOrEmpty(tokens.RequestToken))
        {
            Response.Cookies.Append(
                CookieName,
                tokens.RequestToken,
                new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Path = "/"
                });
        }

        return NoContent();
    }
}
