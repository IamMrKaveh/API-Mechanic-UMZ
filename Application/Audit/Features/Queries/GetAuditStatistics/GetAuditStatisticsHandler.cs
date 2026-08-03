using Application.Audit.Features.Shared;

namespace Application.Audit.Features.Queries.GetAuditStatistics;

public sealed class GetAuditStatisticsHandler(IAuditQueryService auditQueryService)
    : IQueryHandler<GetAuditStatisticsQuery, AuditStatisticsDto>
{
    public async Task<ServiceResult<AuditStatisticsDto>> Handle(
        GetAuditStatisticsQuery request,
        CancellationToken ct)
    {
        var statistics = await auditQueryService.GetStatisticsAsync(request.From, request.To, ct);
        return ServiceResult<AuditStatisticsDto>.Success(statistics);
    }
}
