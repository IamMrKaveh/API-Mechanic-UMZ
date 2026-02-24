namespace Application.Inventory.Features.Queries.GetStatistics;

public record GetInventoryStatisticsQuery : IRequest<ServiceResult<InventoryStatisticsDto>>;