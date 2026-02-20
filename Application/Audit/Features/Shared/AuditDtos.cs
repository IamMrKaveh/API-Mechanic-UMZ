namespace Application.Audit.Features.Shared;

public class AuditDtos
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsArchived { get; set; }
}

public sealed class AuditSearchRequest
{
    public int? UserId { get; set; }
    public string? EventType { get; set; }
    public string? Action { get; set; }
    public string? IpAddress { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string SortBy { get; set; } = "Timestamp";
    public bool SortDesc { get; set; } = true;
}

public sealed class AuditExportRequest
{
    public int? UserId { get; set; }
    public string? EventType { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int MaxRows { get; set; } = 10_000;
}