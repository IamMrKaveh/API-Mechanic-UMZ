namespace Domain.Order.Results;

public sealed record OrderTotals(
    Money Subtotal,
    Money ShippingCost,
    Money DiscountAmount,
    Money FinalAmount,
    int TotalItems);