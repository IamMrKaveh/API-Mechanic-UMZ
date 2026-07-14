namespace Application.Audit.Features.Shared;

public sealed record AuditLogDetailDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string? UserName { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string? Details { get; init; }
    public string? IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime Timestamp { get; init; }
    public bool IsArchived { get; init; }
    public DateTime? ArchivedAt { get; init; }
    public string IntegrityHash { get; init; } = string.Empty;
}