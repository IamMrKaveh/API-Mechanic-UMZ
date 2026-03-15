using Domain.Order.ValueObjects;

namespace Domain.Order.Services;

public class OrderPricingService
{
    public OrderPricingSummary CalculateOrderPricing(
        IEnumerable<OrderItemSnapshot> itemSnapshots,
        Money shippingCost,
        Money discountAmount)
    {
        var itemsList = itemSnapshots.ToList();

        if (!itemsList.Any())
        {
            return new OrderPricingSummary(
                Subtotal: Money.Zero(),
                ShippingCost: shippingCost,
                DiscountAmount: Money.Zero(),
                FinalAmount: shippingCost,
                TotalItems: 0);
        }

        var subtotal = Money.Zero();

        foreach (var item in itemsList)
        {
            var itemAmount = item.UnitPrice.Multiply(item.Quantity);
            subtotal = subtotal.Add(itemAmount);
        }

        var cappedDiscount = discountAmount.IsGreaterThan(subtotal) ? subtotal : discountAmount;

        var beforeDiscount = subtotal.Add(shippingCost);
        var finalAmount = beforeDiscount.IsGreaterThan(cappedDiscount)
            ? beforeDiscount.Subtract(cappedDiscount)
            : Money.Zero(subtotal.Currency);

        var totalItems = itemsList.Sum(i => i.Quantity);

        return new OrderPricingSummary(
            Subtotal: subtotal,
            ShippingCost: shippingCost,
            DiscountAmount: cappedDiscount,
            FinalAmount: finalAmount,
            TotalItems: totalItems);
    }
}

public record OrderPricingSummary(
    Money Subtotal,
    Money ShippingCost,
    Money DiscountAmount,
    Money FinalAmount,
    int TotalItems);