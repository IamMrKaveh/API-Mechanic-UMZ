namespace MainApi.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return null;
        }
    }

    public string? GuestId => _httpContextAccessor.HttpContext?.Request.Headers["X-Guest-Token"].FirstOrDefault();

    public string? IpAddress
    {
        get
        {
            if (_httpContextAccessor.HttpContext?.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) == true)
            {
                var ip = forwardedFor.FirstOrDefault()?.Split(',').FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(ip))
                {
                    return ip;
                }
            }
            return _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
        }
    }

    public bool IsAdmin => _httpContextAccessor.HttpContext?.User.IsInRole("Admin") ?? false;
}