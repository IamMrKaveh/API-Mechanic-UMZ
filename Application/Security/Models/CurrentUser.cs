namespace Application.Security.Models;

public sealed class CurrentUser
{
    public int UserId { get; init; }
    public bool IsAdmin { get; init; }
    private string? PhoneNumber { get; }
    public string IpAddress { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Username { get; init; }
}