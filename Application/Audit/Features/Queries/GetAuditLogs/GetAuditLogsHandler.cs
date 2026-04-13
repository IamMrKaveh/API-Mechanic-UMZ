using Application.Audit.Features.Shared;

namespace Application.Audit.Features.Queries.GetAuditLogs;

public sealed class GetAuditLogsHandler(IAuditQueryService auditQueryService)
    : IRequestHandler<GetAuditLogsQuery, ServiceResult<PaginatedResult<GetAuditLogsResult>>>
{
    public async Task<ServiceResult<PaginatedResult<GetAuditLogsResult>>> Handle(
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

        var resultItems = logs.Select(log => new GetAuditLogsResult
        {
            Logs = [log],
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)total / request.PageSize)
        }).ToList();

        var paginated = PaginatedResult<GetAuditLogsResult>.Create(resultItems, total, request.Page, request.PageSize);

        return ServiceResult<PaginatedResult<GetAuditLogsResult>>.Success(paginated);
    }
}