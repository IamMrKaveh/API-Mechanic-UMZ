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
    public DateTime? Timestamp { get; init; }
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

public sealed record ExportAuditLogsResult(
    byte[] FileContent,
    string FileName = "",
    string ContentType = "text/csv");

public sealed record GetAuditLogsResult
{
    public IReadOnlyList<AuditLogDto> Logs { get; init; } = [];
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

public sealed record AuditExportRequest
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public Guid? UserId { get; init; }
    public string? Action { get; init; }
    public string? EntityName { get; init; }
    public string? EventType { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int MaxRows { get; init; }
}

public sealed record AuditSearchRequest
{
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public Guid? UserId { get; init; }
    public string? Action { get; init; }
    public string? EntityName { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? EventType { get; init; }
    public string? Keyword { get; init; }
    public string? IpAddress { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public bool SortDesc { get; init; }
}