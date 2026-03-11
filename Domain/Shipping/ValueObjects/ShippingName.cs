namespace Domain.Shipping.ValueObjects;

public sealed class ShippingName : ValueObject
{
    public string Value { get; }

    private const int MinLength = 2;
    public const int MaxLength = 100;

    private ShippingName(string value)
    {
        Value = value;
    }

    public static ShippingName Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام روش ارسال الزامی است.");

        var normalized = PersianTextNormalizer.Normalize(name);

        if (normalized.Length < MinLength)
            throw new DomainException($"نام روش ارسال باید حداقل {MinLength} کاراکتر باشد.");

        if (normalized.Length > MaxLength)
            throw new DomainException($"نام روش ارسال نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");

        return new ShippingName(normalized);
    }

    public bool IsSameAs(string other)
    {
        if (string.IsNullOrWhiteSpace(other))
            return false;

        return Value.Equals(other.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;

    public static implicit operator string(ShippingName name) => name.Value;
}