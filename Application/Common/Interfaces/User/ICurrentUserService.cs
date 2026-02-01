namespace Application.Common.Interfaces.User;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? GuestId { get; }
    string? IpAddress { get; }
    bool IsAdmin { get; }
}