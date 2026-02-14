namespace Application.Inventory.Features.Queries.GetInventoryStatus;

public record GetInventoryStatusQuery(int VariantId) : IRequest<ServiceResult<InventoryStatusDto>>;