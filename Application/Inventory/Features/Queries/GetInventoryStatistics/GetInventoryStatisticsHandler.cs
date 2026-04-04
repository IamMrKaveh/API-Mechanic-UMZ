using Application.Common.Results;

namespace Application.Inventory.Features.Queries.GetInventoryStatistics;

public class GetInventoryStatisticsHandler(IInventoryQueryService queryService)
        : IRequestHandler<GetInventoryStatisticsQuery, ServiceResult<InventoryStatisticsDto>>
{
    private readonly IInventoryQueryService _queryService = queryService;

    public async Task<ServiceResult<InventoryStatisticsDto>> Handle(
        GetInventoryStatisticsQuery request,
        CancellationToken ct)
    {
        var stats = await _queryService.GetStatisticsAsync(ct);
        return ServiceResult<InventoryStatisticsDto>.Success(stats);
    }
}