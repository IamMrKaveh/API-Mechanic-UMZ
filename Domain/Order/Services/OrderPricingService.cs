namespace Domain.Order.Services;

/// <summary>
/// سرویس محاسبه قیمت سفارش
/// Stateless Domain Service
/// </summary>
public class OrderPricingService
{
    /// <summary>
    /// محاسبه قیمت‌گذاری سفارش بر اساس آیتم‌ها، روش ارسال و تخفیف
    /// </summary>
    public OrderPricingSummary CalculateOrderPricing(
        IEnumerable<(ProductVariant Variant, int Quantity)> items,
        Shipping.Shipping shippingMethod,
        DiscountCode? discount = null)
    {
        var itemsList = items.ToList();

        
        var subtotal = Money.Zero();
        var totalProfit = Money.Zero();

        foreach (var (variant, quantity) in itemsList)
        {
            var itemAmount = Money.FromDecimal(variant.SellingPrice).Multiply(quantity);
            var itemProfit = Money.FromDecimal(variant.SellingPrice - variant.PurchasePrice).Multiply(quantity);

            subtotal = subtotal.Add(itemAmount);
            totalProfit = totalProfit.Add(itemProfit);
        }

        
        var totalShippingMultiplier = itemsList.Sum(x => x.Variant.ShippingMultiplier);
        var shippingCost = Money.FromDecimal(shippingMethod.BaseCost.Amount * totalShippingMultiplier);

        
        var discountAmount = Money.Zero();
        if (discount != null && discount.IsCurrentlyValid())
        {
            var rawDiscount = discount.CalculateDiscountAmount(subtotal.Amount);
            discountAmount = Money.FromDecimal(rawDiscount);
        }

        
        var finalAmount = subtotal.Add(shippingCost).Subtract(discountAmount);

        return new OrderPricingSummary(
            Subtotal: subtotal,
            ShippingCost: shippingCost,
            DiscountAmount: discountAmount,
            FinalAmount: finalAmount,
            TotalProfit: totalProfit
        );
    }
}

public record OrderPricingSummary(
    Money Subtotal,
    Money ShippingCost,
    Money DiscountAmount,
    Money FinalAmount,
    Money TotalProfit
);