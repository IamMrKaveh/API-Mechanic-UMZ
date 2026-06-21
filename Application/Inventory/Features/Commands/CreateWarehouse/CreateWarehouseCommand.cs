namespace Application.Inventory.Features.Commands.CreateWarehouse;

public record CreateWarehouseCommand(
    string Code,
    string Name,
    string City,
    string? Address,
    string? Phone,
    int Priority,
    bool IsDefault) : ICommand;