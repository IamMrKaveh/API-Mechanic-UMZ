using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetInventoryStatistics;

public class GetInventoryStatisticsHandler(IInventoryQueryService queryService)
    : IQueryHandler<GetInventoryStatisticsQuery, InventoryStatisticsDto>
{
    public async Task<ServiceResult<InventoryStatisticsDto>> Handle(
        GetInventoryStatisticsQuery request,
        CancellationToken ct)
    {
        var statsResult = await (queryService.GetStatisticsAsync(ct)).OrNotFoundAsync("آماری یافت نشد.");
        if (statsResult.IsFailure) return statsResult.Error;
        var stats = statsResult.Value;

        return ServiceResult<InventoryStatisticsDto>.Success(stats);
    }
}