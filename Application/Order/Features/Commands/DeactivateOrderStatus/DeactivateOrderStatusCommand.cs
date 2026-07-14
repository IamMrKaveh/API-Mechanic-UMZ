namespace Application.Order.Features.Commands.DeactivateOrderStatus;

public record DeactivateOrderStatusCommand(
	Guid Id)
	: ICommand, IAuditableCommand
{
	public string AuditEventType => "OrderStatus";

	public string AuditAction => "DeactivateOrderStatus";

	public string? AuditEntityType => "OrderStatus";

	public string? AuditEntityId => Id.ToString();
}