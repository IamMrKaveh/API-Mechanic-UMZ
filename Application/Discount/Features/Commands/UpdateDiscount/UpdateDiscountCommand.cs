using Application.Discount.Features.Shared;
using Domain.Discount.Enums;

namespace Application.Discount.Features.Commands.UpdateDiscount;

public record UpdateDiscountCommand(
	Guid Id,
	DiscountType DiscountType,
	decimal Value,
	decimal? MaximumDiscountAmount,
	int? UsageLimit,
	DateTime? StartsAt,
	DateTime? ExpiresAt,
	bool IsActive) : ICommand<DiscountDto>, IAuditableCommand
{
	public string AuditEventType => "Discount";

	public string AuditAction => "UpdateDiscount";

	public string? AuditEntityType => "DiscountCode";

	public string? AuditEntityId => Id.ToString();
}