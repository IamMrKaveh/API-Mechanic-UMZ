using Application.Common.Interfaces;
using SharedKernel.Models;
using System.Security.Claims;

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
            var value = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAdmin =>
        User?.IsInRole("Admin") ?? false;

    public string? UserAgent =>
        _httpContextAccessor.HttpContext?.Request?.Headers.UserAgent.FirstOrDefault();

    public string? GuestToken =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-Guest-Token"].FirstOrDefault();

    public CurrentUser CurrentUser => new()
    {
        UserId = UserId ?? Guid.Empty,
        IsAdmin = IsAdmin,
        PhoneNumber = User?.FindFirst(ClaimTypes.MobilePhone)?.Value,
        Email = User?.FindFirst(ClaimTypes.Email)?.Value,
        Username = User?.Identity?.Name,
        IpAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? string.Empty
    };
}