namespace Application.Inventory.Features.Commands.ToggleWarehouseActive;

public record ToggleWarehouseActiveCommand(Guid Id, bool IsActive) : IRequest<ServiceResult>;