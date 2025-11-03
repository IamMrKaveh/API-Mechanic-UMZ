namespace MainApi.Controllers.Base;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected readonly string BaseUrl =
        "https://storage.c2.liara.space/mechanic-umz";

    [NonAction]
    protected int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }

        return null;
    }

    [NonAction]
    protected string? ToAbsoluteUrl(string? relativeUrl)
    {
        if (string.IsNullOrEmpty(relativeUrl))
            return null;

        // If it's already a full URL, return it as is.
        if (Uri.IsWellFormedUriString(relativeUrl, UriKind.Absolute))
            return relativeUrl;

        return $"{BaseUrl}{relativeUrl.TrimStart('~')}";
    }

    [NonAction]
    protected string? ToRelativeUrl(string? absoluteUrl)
    {
        if (string.IsNullOrEmpty(absoluteUrl))
            return null;

        if (absoluteUrl.StartsWith(BaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            return absoluteUrl[BaseUrl.Length..];
        }

        return absoluteUrl;
    }
}