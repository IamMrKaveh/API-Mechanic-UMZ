namespace Application.Inventory.Features.Queries.GetInventoryStatus;

public record GetInventoryStatusQuery(Guid VariantId) : IRequest<ServiceResult<InventoryStatusDto>>;