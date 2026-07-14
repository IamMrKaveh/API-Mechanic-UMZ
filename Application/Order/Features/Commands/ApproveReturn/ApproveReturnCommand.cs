namespace Application.Order.Features.Commands.ApproveReturn;

public record ApproveReturnCommand(
	Guid OrderId,
	string Reason = "ØªØ£ÛŒÛŒØ¯ Ù…Ø±Ø¬ÙˆØ¹ÛŒ ØªÙˆØ³Ø· Ø§Ø¯Ù…ÛŒÙ†")
	: ICommand, IBypassTransactionBehavior, IAuditableCommand
{
	public string AuditEventType => "Order";

	public string AuditAction => "ApproveReturn";

	public string? AuditEntityType => "Order";

	public string? AuditEntityId => OrderId.ToString();
}