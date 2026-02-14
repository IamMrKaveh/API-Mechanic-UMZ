namespace Domain.Product.ValueObjects;

public sealed class ProductName : ValueObject
{
    public string Value { get; }

    private const int MinLength = 2;
    private const int MaxLength = 200;

    private ProductName(string value)
    {
        Value = value;
    }

    public static ProductName Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام محصول الزامی است.");

        var normalized = Normalize(name);
        Validate(normalized);

        return new ProductName(normalized);
    }

    private static string Normalize(string name)
    {
        return name.Trim()
            .Replace("ي", "ی")
            .Replace("ك", "ک")
            .Replace("ى", "ی");
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