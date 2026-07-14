namespace Application.Payment.Features.Commands.DeactivatePaymentMethod;

public record DeactivatePaymentMethodCommand(Guid Id) : ICommand, IAuditableCommand
{
	public string AuditEventType => "PaymentMethod";

	public string AuditAction => "DeactivatePaymentMethod";

	public string? AuditEntityType => "PaymentMethod";

	public string? AuditEntityId => Id.ToString();
}