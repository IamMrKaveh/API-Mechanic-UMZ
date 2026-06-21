using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetInventoryStatistics;

public record GetInventoryStatisticsQuery : IQuery<InventoryStatisticsDto>;