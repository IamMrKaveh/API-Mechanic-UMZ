namespace Application.Audit.Features.Shared;

public sealed record AuditLogDto
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
}

public sealed record AuditStatisticsDto
{
    public long TotalLogs { get; init; }
    public IReadOnlyDictionary<string, long> ByEventType { get; init; } = new Dictionary<string, long>();
    public IReadOnlyDictionary<string, long> ByHour { get; init; } = new Dictionary<string, long>();
}

public sealed record ExportAuditLogsResult(byte[] FileContent, string FileName, string ContentType);

public sealed record EventTypeCountDto(
    string EventType,
    int Count);

public sealed record HourlyCountDto(
    int Hour,
    int Count);

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