namespace Domain.Payment.ValueObjects;

public sealed class PaymentMethodName : ValueObject
{
    public string Value { get; }
    private const int MinLength = 2;
    public const int MaxLength = 100;

    private PaymentMethodName(string value) => Value = value;

    public static PaymentMethodName Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام روش پرداخت الزامی است.");
        if (name.Length < MinLength)
            throw new DomainException($"نام روش پرداخت باید حداقل {MinLength} کاراکتر باشد.");
        if (name.Length > MaxLength)
            throw new DomainException($"نام روش پرداخت نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");
        return new PaymentMethodName(name);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;

    public static implicit operator string(PaymentMethodName name) => name.Value;
}