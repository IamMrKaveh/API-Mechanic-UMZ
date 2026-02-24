namespace Application.Audit.Features.Shared;

public class AuditDtos
{
    public int Id { get; init; }
    public int? UserId { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public string? UserAgent { get; init; }
    public DateTime Timestamp { get; init; }
    public bool IsArchived { get; init; }
}

public sealed class AuditSearchRequest
{
    public int? UserId { get; init; }
    public string? EventType { get; init; }
    public string? Action { get; init; }
    public string? IpAddress { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string? Keyword { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public string SortBy { get; init; } = "Timestamp";
    public bool SortDesc { get; init; } = true;
}

public sealed class AuditExportRequest
{
    public int? UserId { get; init; }
    public string? EventType { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int MaxRows { get; init; } = 10_000;
}