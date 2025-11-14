namespace Application.Common.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    string? GuestId { get; }
    string? IpAddress { get; }
    bool IsAdmin { get; }
}