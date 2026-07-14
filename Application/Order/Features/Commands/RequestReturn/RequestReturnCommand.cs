namespace Application.Order.Features.Commands.RequestReturn;

public sealed record RequestReturnCommand(
	Guid OrderId,
	Guid UserId,
	string Reason,
	string RowVersion)
	: ICommand, IAuditableCommand
{
	public string AuditEventType => "Order";

	public string AuditAction => "RequestReturn";

	public string? AuditEntityType => "Order";

	public string? AuditEntityId => OrderId.ToString();
}