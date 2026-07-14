namespace Application.Inventory.Features.Commands.SetDefaultWarehouse;

public record SetDefaultWarehouseCommand(
	Guid Id) : ICommand, IAuditableCommand
{
	public string AuditEventType => "Warehouse";

	public string AuditAction => "SetDefaultWarehouse";

	public string? AuditEntityType => "Warehouse";

	public string? AuditEntityId => Id.ToString();
}