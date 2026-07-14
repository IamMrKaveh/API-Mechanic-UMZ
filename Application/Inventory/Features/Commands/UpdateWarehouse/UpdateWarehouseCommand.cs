namespace Application.Inventory.Features.Commands.UpdateWarehouse;

public record UpdateWarehouseCommand(
	Guid Id,
	string Name,
	string City,
	string? Address,
	string? Phone,
	int Priority) : ICommand, IAuditableCommand
{
	public string AuditEventType => "Warehouse";

	public string AuditAction => "UpdateWarehouse";

	public string? AuditEntityType => "Warehouse";

	public string? AuditEntityId => Id.ToString();
}