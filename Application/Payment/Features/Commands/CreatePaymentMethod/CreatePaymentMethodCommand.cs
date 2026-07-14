using Application.Payment.Features.Shared;

namespace Application.Payment.Features.Commands.CreatePaymentMethod;

public record CreatePaymentMethodCommand(
	string Name,
	string Code,
	string? Description,
	string? IconUrl,
	decimal FeeAmount,
	decimal FeePercentage,
	int SortOrder)
	: ICommand<PaymentMethodDto>, IAuditableCommand
{
	public string AuditEventType => "PaymentMethod";

	public string AuditAction => "CreatePaymentMethod";

	public string? AuditEntityType => "PaymentMethod";

	public string? AuditEntityId => null;
}