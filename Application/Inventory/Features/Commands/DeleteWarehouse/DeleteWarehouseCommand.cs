namespace Application.Inventory.Features.Commands.DeleteWarehouse;

public record DeleteWarehouseCommand(
	Guid Id) : ICommand, IAuditableCommand
{
	public string AuditEventType => "Warehouse";

	public string AuditAction => "DeleteWarehouse";

	public string? AuditEntityType => "Warehouse";

	public string? AuditEntityId => Id.ToString();
}