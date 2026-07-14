namespace Application.Inventory.Features.Commands.CreateWarehouse;

public record CreateWarehouseCommand(
	string Code,
	string Name,
	string City,
	string? Address,
	string? Phone,
	int Priority,
	bool IsDefault) : ICommand, IAuditableCommand
{
	public string AuditEventType => "Warehouse";

	public string AuditAction => "CreateWarehouse";

	public string? AuditEntityType => "Warehouse";

	public string? AuditEntityId => null;
}