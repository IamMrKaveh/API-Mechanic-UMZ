namespace Application.Order.Features.Commands.DeleteOrderStatus;

public record DeleteOrderStatusCommand(
	Guid Id)
	: ICommand, IAuditableCommand
{
	public string AuditEventType => "OrderStatus";

	public string AuditAction => "DeleteOrderStatus";

	public string? AuditEntityType => "OrderStatus";

	public string? AuditEntityId => Id.ToString();
}