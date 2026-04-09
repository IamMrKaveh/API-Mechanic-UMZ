using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetInventory;

public record GetInventoryQuery(Guid VariantId) : IRequest<ServiceResult<InventoryDto>>;