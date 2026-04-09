using SharedKernel.Models;

namespace SharedKernel.Contracts;

public interface ICurrentUserService
{
    CurrentUser CurrentUser { get; }
    bool IsAuthenticated { get; }
    string? UserAgent { get; }
    string? GuestId { get; }
    Guid? UserId => IsAuthenticated ? CurrentUser.UserId : null;
}