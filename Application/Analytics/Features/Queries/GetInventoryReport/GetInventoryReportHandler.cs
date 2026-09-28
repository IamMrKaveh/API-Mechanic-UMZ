using Application.Analytics.Features.Shared;

namespace Application.Analytics.Features.Queries.GetInventoryReport;

public sealed class GetInventoryReportHandler(
    IAnalyticsQueryService analyticsQuery)
    : IQueryHandler<GetInventoryReportQuery, InventoryReportDto>
{
    public async Task<ServiceResult<InventoryReportDto>> Handle(
        GetInventoryReportQuery request,
        CancellationToken ct)
    {
        var result = await analyticsQuery.GetInventoryReportAsync(ct);

        return ServiceResult<InventoryReportDto>.Success(result);
    }
}