namespace Application.Order.Features.Commands.CancelOrder;

public record CancelOrderCommand(
	Guid OrderId,
	string Reason)
	: ICommand, IAuditableCommand
{
	public string AuditEventType => "Order";

	public string AuditAction => "CancelOrder";

	public string? AuditEntityType => "Order";

	public string? AuditEntityId => OrderId.ToString();
}