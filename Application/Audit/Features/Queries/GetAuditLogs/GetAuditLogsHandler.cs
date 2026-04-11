using Application.Audit.Features.Shared;

namespace Application.Audit.Features.Queries.GetAuditLogs;

public sealed class GetAuditLogsHandler(IAuditQueryService auditQueryService) : IRequestHandler<GetAuditLogsQuery, GetAuditLogsResult>
{
    public async Task<GetAuditLogsResult> Handle(
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

        var totalPages = (int)Math.Ceiling((double)total / request.PageSize);

        return new GetAuditLogsResult(
            logs,
            total,
            request.Page,
            request.PageSize,
            totalPages);
    }
}