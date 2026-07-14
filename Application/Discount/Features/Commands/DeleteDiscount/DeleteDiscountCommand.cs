namespace Application.Discount.Features.Commands.DeleteDiscount;

public record DeleteDiscountCommand(
	Guid Id) : ICommand, IAuditableCommand
{
	public string AuditEventType => "Discount";

	public string AuditAction => "DeleteDiscount";

	public string? AuditEntityType => "DiscountCode";

	public string? AuditEntityId => Id.ToString();
}