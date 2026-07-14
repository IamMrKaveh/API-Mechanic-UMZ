namespace Application.Order.Features.Commands.SetDefaultOrderStatus;

public record SetDefaultOrderStatusCommand(
	Guid Id)
	: ICommand, IAuditableCommand
{
	public string AuditEventType => "OrderStatus";

	public string AuditAction => "SetDefaultOrderStatus";

	public string? AuditEntityType => "OrderStatus";

	public string? AuditEntityId => Id.ToString();
}