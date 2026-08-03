using Application.Audit.Features.Shared;

namespace Application.Audit.Features.Queries.ExportAuditLogs;

public sealed class ExportAuditLogsHandler(
    IAuditQueryService auditQueryService)
    : IQueryHandler<ExportAuditLogsQuery, ExportAuditLogsResult>
{
    private const int DefaultCsvMaxRows = 10_000;
    private const int DefaultJsonMaxRows = 5_000;

    public async Task<ServiceResult<ExportAuditLogsResult>> Handle(
        ExportAuditLogsQuery request,
        CancellationToken ct)
    {
        var isJson = request.Format.Equals("json", StringComparison.OrdinalIgnoreCase);

        var effectiveMaxRows = request.MaxRows
            ?? (isJson ? DefaultJsonMaxRows : DefaultCsvMaxRows);

        var exportRequest = new AuditExportRequest
        {
            UserId = request.UserId,
            EventType = request.EventType,
            EntityType = request.EntityType,
            From = request.From,
            To = request.To,
            MaxRows = effectiveMaxRows
        };

        var (contentType, extension) = isJson
            ? ("application/json", "json")
            : ("text/csv", "csv");

        var content = isJson
            ? await auditQueryService.ExportToJsonAsync(exportRequest, ct)
            : await auditQueryService.ExportToCsvAsync(exportRequest, ct);

        var fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmm}.{extension}";

        return ServiceResult<ExportAuditLogsResult>.Success(
            new ExportAuditLogsResult(content, fileName, contentType));
    }
}
