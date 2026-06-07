using Application.Common.Interfaces;
using SharedKernel.Constants;

namespace Presentation.Common.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

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
}