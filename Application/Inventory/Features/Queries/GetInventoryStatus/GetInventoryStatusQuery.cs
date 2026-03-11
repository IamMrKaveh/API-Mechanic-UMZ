using Application.Common.Models;

namespace Application.Inventory.Features.Queries.GetInventoryStatus;

public record GetInventoryStatusQuery(int VariantId) : IRequest<ServiceResult<InventoryStatusDto>>;