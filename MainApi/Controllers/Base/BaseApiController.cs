namespace MainApi.Controllers.Base;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected readonly string BaseUrl = "https://storage.c2.liara.space/mechanic-umz";

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

        if (Uri.IsWellFormedUriString(relativeUrl, UriKind.Absolute))
            return relativeUrl;

        var cleanRelative = relativeUrl.TrimStart('~', '/');
        return $"{BaseUrl}/{cleanRelative}";
    }

    [NonAction]
    protected string? ToRelativeUrl(string? absoluteUrl)
    {
        if (string.IsNullOrEmpty(absoluteUrl))
            return absoluteUrl;

        if (absoluteUrl.StartsWith(BaseUrl, StringComparison.OrdinalIgnoreCase))
        {
            var relative = absoluteUrl.Substring(BaseUrl.Length);
            return relative.StartsWith("/") ? relative : $"/{relative}";
        }

        return absoluteUrl.StartsWith("/") ? absoluteUrl : $"/{absoluteUrl}";
    }
}