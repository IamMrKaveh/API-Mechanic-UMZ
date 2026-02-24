namespace Application.Inventory.Features.Queries.GetStatistics;

public class GetInventoryStatisticsHandler
    : IRequestHandler<GetInventoryStatisticsQuery, ServiceResult<InventoryStatisticsDto>>
{
    private readonly IInventoryQueryService _queryService;

    public GetInventoryStatisticsHandler(IInventoryQueryService queryService)
    {
        _queryService = queryService;
    }

    public async Task<ServiceResult<InventoryStatisticsDto>> Handle(
        GetInventoryStatisticsQuery request,
        CancellationToken cancellationToken)
    {
        var stats = await _queryService.GetStatisticsAsync(cancellationToken);
        return ServiceResult<InventoryStatisticsDto>.Success(stats);
    }
}