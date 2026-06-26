namespace Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    string? GuestToken { get; }
    string FrontendBaseUrl { get; }
}