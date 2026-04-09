namespace Application.Common.Interfaces;

public interface ICurrentUserService
{
    CurrentUser CurrentUser { get; }
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    bool IsAdmin { get; }
    string? UserAgent { get; }
    string? GuestToken { get; }
}