namespace Domain.Order.Results;

/// <summary>
/// نتیجه محاسبه مجموع سفارش
/// </summary>
public sealed record OrderTotals(
    Money Subtotal,
    Money Profit,
    Money ShippingCost,
    Money DiscountAmount,
    Money FinalAmount,
    int TotalItems);