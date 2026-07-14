namespace Application.Order.Features.Commands.UpdateOrderStatus;

public record UpdateOrderStatusCommand(
	Guid OrderId,
	string NewStatus,
	string RowVersion)
	: ICommand, IAuditableCommand
{
	public string AuditEventType => "Order";

	public string AuditAction => "UpdateOrderStatus";

	public string? AuditEntityType => "Order";

	public string? AuditEntityId => OrderId.ToString();
}