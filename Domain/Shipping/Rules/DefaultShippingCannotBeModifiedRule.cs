namespace Domain.Shipping.Rules;

public sealed class DefaultShippingCannotBeModifiedRule(Aggregates.Shipping shipping, string operation) : IBusinessRule
{
    private readonly Aggregates.Shipping _shipping = shipping;
    private readonly string _operation = operation;

    public bool IsBroken()
    {
        return _shipping.IsDefault;
    }

    public string Message => $"امکان {_operation} روش ارسال پیش‌فرض وجود ندارد.";
}