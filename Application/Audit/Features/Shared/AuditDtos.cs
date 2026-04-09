namespace Application.Audit.Features.Shared;

public record AuditLogDto
{
    public Guid Id { get; init; }
    public Guid? UserId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string? Details { get; init; }
    public string IpAddress { get; init; } = string.Empty;
    public string? UserAgent { get; init; }
    public string? EntityType { get; init; }
    public Guid? EntityId { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsArchived { get; init; }
}

public sealed record AuditStatisticsDto(
    int TotalLogs,
    int FinancialLogs,
    int SecurityLogs,
    int AdminLogs,
    IEnumerable<EventTypeCountDto> ByEventType,
    IEnumerable<HourlyCountDto> ByHour);

public sealed record EventTypeCountDto(
    string EventType,
    int Count);

public sealed record HourlyCountDto(
    int Hour,
    int Count);