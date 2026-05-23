namespace Application.Inventory.Features.Commands.UpdateWarehouse;

public record UpdateWarehouseCommand(
    Guid Id,
    string Name,
    string City,
    string? Address,
    string? Phone,
    int Priority) : IRequest<ServiceResult>;