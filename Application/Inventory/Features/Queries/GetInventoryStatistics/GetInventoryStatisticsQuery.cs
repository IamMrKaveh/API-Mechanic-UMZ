using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetInventoryStatistics;

public record GetInventoryStatisticsQuery : IRequest<ServiceResult<InventoryStatisticsDto>>;