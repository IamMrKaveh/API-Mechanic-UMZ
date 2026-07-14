using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Commands.UpdatePaymentMethod;

public record UpdatePaymentMethodCommand(
	Guid Id,
	string Name,
	string? Description,
	string? IconUrl,
	decimal FeeAmount,
	decimal FeePercentage,
	int SortOrder)
	: ICommand<PaymentMethodDto>, IAuditableCommand
{
	public string AuditEventType => "PaymentMethod";

	public string AuditAction => "UpdatePaymentMethod";

	public string? AuditEntityType => "PaymentMethod";

	public string? AuditEntityId => Id.ToString();
}