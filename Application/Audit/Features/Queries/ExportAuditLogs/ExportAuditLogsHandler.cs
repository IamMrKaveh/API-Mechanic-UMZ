namespace Application.Audit.Features.Queries.ExportAuditLogs;

public sealed class ExportAuditLogsHandler : IRequestHandler<ExportAuditLogsQuery, ExportAuditLogsResult>
{
    private readonly IAuditService _auditService;

    public ExportAuditLogsHandler(IAuditService auditService)
        => _auditService = auditService;

    public async Task<ExportAuditLogsResult> Handle(
        ExportAuditLogsQuery request,
        CancellationToken ct)
    {
        var exportRequest = new AuditExportRequest
        {
            UserId = request.UserId,
            EventType = request.EventType,
            From = request.From,
            To = request.To,
            MaxRows = request.MaxRows
        };

        var (contentType, extension) = request.Format.ToLowerInvariant() switch
        {
            "json" => ("application/json", "json"),
            _ => ("text/csv", "csv")
        };

        byte[] content;
        if (request.Format == "json")
        {
            var (logs, _) = await _auditService.GetAuditLogsAsync(
                request.UserId, request.EventType,
                request.From, request.To,
                page: 1, pageSize: request.MaxRows);

            content = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(logs,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        else
        {
            content = await _auditService.ExportToCsvAsync(exportRequest, ct);
        }

        var fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmm}.{extension}";

        return new ExportAuditLogsResult(content, contentType, fileName);
    }
}