namespace Application.Inventory.Features.Commands.ToggleWarehouseActive;

public record ToggleWarehouseActiveCommand(
	Guid Id,
	bool IsActive) : ICommand, IAuditableCommand
{
	public string AuditEventType => "Warehouse";

	public string AuditAction => IsActive ? "ActivateWarehouse" : "DeactivateWarehouse";

	public string? AuditEntityType => "Warehouse";

	public string? AuditEntityId => Id.ToString();
}