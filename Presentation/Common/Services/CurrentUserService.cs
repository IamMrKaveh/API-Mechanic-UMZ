using Application.Common.Interfaces;
using Microsoft.IdentityModel.JsonWebTokens;
using SharedKernel.Constants;

namespace Presentation.Common.Services;

public class CurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IConfiguration _configuration = configuration;

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var value = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User?.FindFirst("nameid")?.Value
                ?? User?.FindFirst("sub")?.Value;

            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? SessionId
    {
        get
        {
            var value = User?.FindFirst("sid")?.Value
                ?? User?.FindFirst(JwtRegisteredClaimNames.Sid)?.Value;

            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAdmin =>
        User?.IsInRole(AppRoles.Admin) ?? false
        || (User?.FindFirst(ClaimTypes.Role)?.Value == AppRoles.Admin)
        || (User?.FindFirst("role")?.Value == AppRoles.Admin)
        || (User?.Claims
               .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
               .Any(c => c.Value == AppRoles.Admin) ?? false);

    public string? IpAddress =>
        _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

    public string? UserAgent =>
        _httpContextAccessor.HttpContext?.Request?.Headers.UserAgent.FirstOrDefault();

    public string? GuestToken =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-Guest-Token"].FirstOrDefault();

    public string FrontendBaseUrl
    {
        get
        {
            var origin = _httpContextAccessor.HttpContext?.Request.Headers.Origin.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(origin))
            {
                var allowedOrigins = _configuration
                    .GetSection("Security:AllowedOrigins")
                    .Get<string[]>() ?? [];

                if (allowedOrigins.Contains(origin.TrimEnd('/'), StringComparer.OrdinalIgnoreCase))
                    return origin.TrimEnd('/');
            }

            return (_configuration["FrontendUrls:BaseUrl"] ?? "https://ledka-co.ir").TrimEnd('/');
        }
    }
}