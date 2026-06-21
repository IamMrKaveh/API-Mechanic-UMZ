namespace Application.Discount.Features.Commands.ApplyDiscount;

public record ApplyDiscountCommand(
    string Code,
    decimal OrderAmount,
    Guid OrderId) : ICommand, IBypassTransactionBehavior;