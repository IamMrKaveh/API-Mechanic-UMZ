using Application.Common.Models;

namespace Application.Inventory.Features.Queries.GetInventoryStatistics;

public record GetInventoryStatisticsQuery : IRequest<ServiceResult<InventoryStatisticsDto>>;