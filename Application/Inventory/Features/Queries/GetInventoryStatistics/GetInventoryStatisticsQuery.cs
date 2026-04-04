using Application.Common.Results;

namespace Application.Inventory.Features.Queries.GetInventoryStatistics;

public record GetInventoryStatisticsQuery : IRequest<ServiceResult<InventoryStatisticsDto>>;