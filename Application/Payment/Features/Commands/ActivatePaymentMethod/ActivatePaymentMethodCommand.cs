namespace Application.Payment.Features.Commands.ActivatePaymentMethod;

public record ActivatePaymentMethodCommand(Guid Id) : ICommand, IAuditableCommand
{
	public string AuditEventType => "PaymentMethod";

	public string AuditAction => "ActivatePaymentMethod";

	public string? AuditEntityType => "PaymentMethod";

	public string? AuditEntityId => Id.ToString();
}