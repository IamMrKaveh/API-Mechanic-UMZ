using Application.Audit.Features.Shared;

namespace Application.Audit.Features.Queries.GetAuditStatistics;

public sealed class GetAuditStatisticsHandler(IAuditQueryService auditQueryService)
    : IQueryHandler<GetAuditStatisticsQuery, PaginatedResult<AuditStatisticsDto>>
{
    public async Task<ServiceResult<PaginatedResult<AuditStatisticsDto>>> Handle(
        GetAuditStatisticsQuery request,
        CancellationToken ct)
    {
        var statistics = await auditQueryService.GetStatisticsAsync(request.From, request.To, ct);

        var paginated = PaginatedResult<AuditStatisticsDto>.Create([statistics], 1, 1, 1);

        return ServiceResult<PaginatedResult<AuditStatisticsDto>>.Success(paginated);
    }
}