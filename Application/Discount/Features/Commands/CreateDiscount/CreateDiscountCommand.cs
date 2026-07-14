using Application.Discount.Features.Shared;
using Domain.Discount.Enums;

namespace Application.Discount.Features.Commands.CreateDiscount;

public record CreateDiscountCommand(
	string Code,
	DiscountType DiscountType,
	decimal Value,
	decimal? MaximumDiscountAmount,
	int? UsageLimit,
	bool? IsActive,
	DateTime? StartsAt,
	DateTime? ExpiresAt) : ICommand<DiscountDto>, IAuditableCommand
{
	public string AuditEventType => "Discount";

	public string AuditAction => "CreateDiscount";

	public string? AuditEntityType => "DiscountCode";

	public string? AuditEntityId => null;
}