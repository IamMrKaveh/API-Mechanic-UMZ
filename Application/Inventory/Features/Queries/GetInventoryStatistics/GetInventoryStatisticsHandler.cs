using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetInventoryStatistics;

public class GetInventoryStatisticsHandler(IInventoryQueryService queryService)
    : IRequestHandler<GetInventoryStatisticsQuery, ServiceResult<InventoryStatisticsDto>>
{
    public async Task<ServiceResult<InventoryStatisticsDto>> Handle(
        GetInventoryStatisticsQuery request,
        CancellationToken ct)
    {
        var stats = await queryService.GetStatisticsAsync(ct);

        if (stats is null)
            return ServiceResult<InventoryStatisticsDto>.NotFound("آماری یافت نشد.");

        return ServiceResult<InventoryStatisticsDto>.Success(stats);
    }
}