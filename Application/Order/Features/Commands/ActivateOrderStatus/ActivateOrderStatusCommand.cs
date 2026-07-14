namespace Application.Order.Features.Commands.ActivateOrderStatus;

public record ActivateOrderStatusCommand(
	Guid Id)
	: ICommand, IAuditableCommand
{
	public string AuditEventType => "OrderStatus";

	public string AuditAction => "ActivateOrderStatus";

	public string? AuditEntityType => "OrderStatus";

	public string? AuditEntityId => Id.ToString();
}