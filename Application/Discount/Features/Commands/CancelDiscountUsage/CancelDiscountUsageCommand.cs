namespace Application.Discount.Features.Commands.CancelDiscountUsage;

public record CancelDiscountUsageCommand(
    Guid DiscountCodeId,
    Guid OrderId) : ICommand;