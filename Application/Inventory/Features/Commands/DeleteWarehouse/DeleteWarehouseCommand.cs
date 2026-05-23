namespace Application.Inventory.Features.Commands.DeleteWarehouse;

public record DeleteWarehouseCommand(Guid Id) : IRequest<ServiceResult>;