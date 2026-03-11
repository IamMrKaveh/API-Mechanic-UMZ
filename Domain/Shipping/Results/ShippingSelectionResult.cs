namespace Domain.Shipping.Results;

public sealed class ShippingSelectionResult
{
    public bool IsSuccess { get; private set; }
    public Aggregates.Shipping? SelectedShipping { get; private set; }
    public string? Error { get; private set; }

    private ShippingSelectionResult()
    { }

    public static ShippingSelectionResult Selected(Aggregates.Shipping shipping) =>
        new()
        {
            IsSuccess = true,
            SelectedShipping = shipping
        };

    public static ShippingSelectionResult NoAvailableShipping() =>
        new()
        {
            IsSuccess = false,
            Error = "هیچ روش ارسال فعالی برای این سفارش در دسترس نیست."
        };
}