using Domain.Shipping.Results;
using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Services;

public sealed class ShippingDomainService
{
    public static ShippingCostCalculationResult CalculateShippingCost(
        Aggregates.Shipping shipping,
        Money orderTotal,
        IEnumerable<ShippingCostItem>? items = null)
    {
        Guard.Against.Null(shipping, nameof(shipping));
        Guard.Against.Null(orderTotal, nameof(orderTotal));

        if (!shipping.IsActive)
            return ShippingCostCalculationResult.NotAvailable(shipping.Id, "روش ارسال غیرفعال است.");

        var validationResult = shipping.ValidateForOrder(orderTotal);
        if (validationResult.IsFailure)
            return ShippingCostCalculationResult.NotAvailable(shipping.Id, validationResult.Error.Message!);

        Money cost;
        bool isFree = shipping.QualifiesForFreeShipping(orderTotal);

        if (items is not null)
        {
            var itemList = items.ToList();
            if (itemList.Count > 0)
            {
                cost = shipping.CalculateCostForCart(orderTotal, itemList);
            }
            else
            {
                cost = shipping.CalculateCost(orderTotal);
            }
        }
        else
        {
            cost = shipping.CalculateCost(orderTotal);
        }

        return ShippingCostCalculationResult.Success(
            shipping.Id,
            cost,
            isFree,
            shipping.GetDeliveryTimeDisplay());
    }
}