using Application.Audit.Features.Shared;
using Domain.Audit.Interfaces;
using Domain.User.ValueObjects;

namespace Infrastructure.Audit.Services;

public sealed class EnhancedAuditService(
    IAuditRepository auditRepository,
    IAuditQueryService auditQueryService,
    IAuditMaskingService masking,
    IUnitOfWork unitOfWork)
{
    public async Task<PaginatedResult<AuditLogDto>> GetAuditLogsPagedAsync(
        Guid? userId,
        string? eventType,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var uid = userId.HasValue ? UserId.From(userId.Value) : null;
        return await auditQueryService.GetAuditLogsAsync(uid, eventType, null, fromDate, toDate, page, pageSize, ct);
    }

    public async Task<(IReadOnlyList<AuditLogDto> Logs, int Total)> SearchLogsAsync(
        AuditSearchRequest request,
        CancellationToken ct = default)
    {
        return await auditQueryService.SearchAsync(request, ct);
    }

    public async Task<byte[]> ExportAsync(AuditExportRequest request, CancellationToken ct = default)
    {
        return await auditQueryService.ExportToCsvAsync(request, ct);
    }
}