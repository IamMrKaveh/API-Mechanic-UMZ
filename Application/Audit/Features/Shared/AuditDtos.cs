namespace Application.Audit.Features.Shared;

public record AuditDtos
{
    public int Id { get; init; }
    public int? UserId { get; init; }
    public string EventType { get; init; }
    public string Action { get; init; }
    public string Details { get; init; }
    public string IpAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTime Timestamp { get; init; }
    public bool IsArchived { get; init; }
}

public sealed record AuditSearchRequest
{
    public int? UserId { get; init; }
    public string? EventType { get; init; }
    public string? Action { get; init; }
    public string? IpAddress { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string? Keyword { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public string SortBy { get; init; }
    public bool SortDesc { get; init; }
}

public sealed record AuditExportRequest(
    int? UserId,
    string? EventType,
    DateTime? From,
    DateTime? To,
    int MaxRows
);