using Application.Audit.Features.Shared;
using Domain.User.ValueObjects;

namespace Application.Audit.Features.Queries.ExportAuditLogs;

public sealed class ExportAuditLogsHandler(
    IAuditQueryService auditQueryService) : IRequestHandler<ExportAuditLogsQuery, ServiceResult<PaginatedResult<ExportAuditLogsResult>>>
{
    public async Task<ServiceResult<PaginatedResult<ExportAuditLogsResult>>> Handle(
        ExportAuditLogsQuery request,
        CancellationToken ct)
    {
        var userId = UserId.From(request.UserId.Value);

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
            var result = await auditQueryService.GetAuditLogsAsync(
                userId,
                request.EventType,
                request.EntityType,
                request.From,
                request.To,
                1,
                request.MaxRows,
                ct);

            content = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(result,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        else
        {
            content = await auditQueryService.ExportToCsvAsync(exportRequest, ct);
        }

        var fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmm}.{extension}";

        return ServiceResult<PaginatedResult<ExportAuditLogsResult>>.Success(new ExportAuditLogsResult(content, fileName, contentType));
    }
}