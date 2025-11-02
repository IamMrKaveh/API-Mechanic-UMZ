namespace MainApi.Controllers.Security;

[Route("api/[controller]")]
[ApiController]
public class SecurityController : BaseApiController
{
    private readonly IAntiforgery _antiforgery;
    public SecurityController(IAntiforgery antiforgery)
    {
        _antiforgery = antiforgery;
    }

    [HttpGet("csrf-token")]
    [IgnoreAntiforgeryToken]
    public IActionResult GetCsrfToken()
    {
        var tokens = _antiforgery.GetAndStoreTokens(HttpContext);
        Response.Cookies.Append("XSRF-TOKEN", tokens.RequestToken!,
                    new CookieOptions
                    {
                        HttpOnly = false,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,

                        Expires = DateTimeOffset.UtcNow.AddHours(1)
                    });
        return Ok(new { token = tokens.RequestToken });
    }
}