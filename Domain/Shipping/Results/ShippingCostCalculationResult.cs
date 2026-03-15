using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Results;

public sealed class ShippingCostCalculationResult
{
    public bool IsSuccess { get; private set; }
    public ShippingId ShippingId { get; private set; } = default!;
    public Money? Cost { get; private set; }
    public bool IsFreeShipping { get; private set; }
    public string? DeliveryTimeDisplay { get; private set; }
    public string? Error { get; private set; }

    private ShippingCostCalculationResult()
    { }

    public static ShippingCostCalculationResult Success(
        ShippingId shippingId,
        Money cost,
        bool isFreeShipping,
        string deliveryTimeDisplay) =>
        new()
        {
            IsSuccess = true,
            ShippingId = shippingId,
            Cost = cost,
            IsFreeShipping = isFreeShipping,
            DeliveryTimeDisplay = deliveryTimeDisplay
        };

    public static ShippingCostCalculationResult NotAvailable(ShippingId shippingId, string error) =>
        new()
        {
            IsSuccess = false,
            ShippingId = shippingId,
            Error = error
        };
}