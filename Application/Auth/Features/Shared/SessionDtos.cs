namespace Application.Auth.Features.Shared;

public record UserSessionDto
{
    public Guid Id { get; init; }
    public string CreatedByIp { get; init; } = string.Empty;
    public string DeviceInfo { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? LastActivityAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public string? SessionType { get; init; }
    public string? BrowserInfo { get; init; }
    public string? PlatformInfo { get; init; }
    public bool IsCurrent { get; init; }
    public long RemainingSeconds { get; init; }
    public bool IsExpiringSoon { get; init; }
}

public record CurrentSessionDto
{
    public Guid? SessionId { get; init; }
    public Guid? UserId { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public bool IsAuthenticated { get; init; }
    public bool IsAdmin { get; init; }
}
