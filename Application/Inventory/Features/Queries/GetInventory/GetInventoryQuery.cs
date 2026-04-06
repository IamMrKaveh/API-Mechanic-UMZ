using Application.Common.Results;
using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetInventory;

public record GetInventoryQuery(int VariantId) : IRequest<ServiceResult<InventoryDto>>;