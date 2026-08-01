using Application.Audit.Features.Shared;

namespace Application.Audit.Features.Queries.GetAuditLogs;

public sealed class GetAuditLogsHandler(IAuditQueryService auditQueryService)
    : IQueryHandler<GetAuditLogsQuery, PaginatedResult<AuditLogDto>>
{
    public async Task<ServiceResult<PaginatedResult<AuditLogDto>>> Handle(
        GetAuditLogsQuery request,
        CancellationToken ct)
    {
        var searchRequest = new AuditSearchRequest
        {
            UserId = request.UserId,
            EventType = request.EventType,
            Action = request.Action,
            Keyword = request.Keyword,
            IpAddress = request.IpAddress,
            From = request.From,
            To = request.To,
            Page = request.Page,
            PageSize = request.PageSize,
            SortDesc = request.SortDesc
        };

        var (logs, total) = await auditQueryService.SearchAsync(searchRequest, ct);

        var paginated = PaginatedResult<AuditLogDto>.Create(
            logs.ToList(),
            total,
            request.Page,
            request.PageSize);

        return ServiceResult<PaginatedResult<AuditLogDto>>.Success(paginated);
    }
}
