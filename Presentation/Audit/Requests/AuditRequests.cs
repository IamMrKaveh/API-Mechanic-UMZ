namespace Presentation.Audit.Requests;

public record GetAuditLogsRequest(
    Guid? UserId = null,
    string? EventType = null,
    string? Action = null,
    string? Keyword = null,
    string? IpAddress = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 50,
    string SortBy = "Timestamp",
    bool SortDesc = true);

public record GetAuditStatisticsRequest(
    DateTime? From = null,
    DateTime? To = null);

public record ExportAuditLogsRequest(
    Guid? UserId = null,
    string? EventType = null,
    string? EntityType = null,
    DateTime? From = null,
    DateTime? To = null,
    int? MaxRows = null);