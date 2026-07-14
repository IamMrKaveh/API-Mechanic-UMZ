namespace Application.Payment.Features.Commands.DeletePaymentMethod;

public record DeletePaymentMethodCommand(Guid Id) : ICommand, IAuditableCommand
{
	public string AuditEventType => "PaymentMethod";

	public string AuditAction => "DeletePaymentMethod";

	public string? AuditEntityType => "PaymentMethod";

	public string? AuditEntityId => Id.ToString();
}