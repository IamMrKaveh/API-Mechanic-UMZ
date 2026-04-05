namespace SharedKernel.Models;

public sealed class CurrentUser
{
    public Guid UserId { get; init; }
    public bool IsAdmin { get; init; }
    public string? PhoneNumber { get; init; }
    public string IpAddress { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Username { get; init; }
}