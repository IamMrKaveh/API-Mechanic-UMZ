namespace Application.Inventory.Features.Commands.SetDefaultWarehouse;

public record SetDefaultWarehouseCommand(
    Guid Id) : ICommand;