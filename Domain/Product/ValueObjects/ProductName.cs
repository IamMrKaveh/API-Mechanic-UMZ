namespace Domain.Product.ValueObjects;

public sealed class ProductName : ValueObject
{
    public string Value { get; }

    private const int MinLength = 2;
    public const int MaxLength = 100;

    private ProductName(string value)
    {
        Value = value;
    }

    public static ProductName Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام محصول الزامی است.");

        //move to application
        //var normalized = PersianTextNormalizer.Normalize(name);
        Validate(name);

        return new ProductName(name);
    }

    private static void Validate(string name)
    {
        if (name.Length < MinLength)
            throw new DomainException($"نام محصول باید حداقل {MinLength} کاراکتر باشد.");

        if (name.Length > MaxLength)
            throw new DomainException($"نام محصول نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;

    public static implicit operator string(ProductName name) => name.Value;
}