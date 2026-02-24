namespace Domain.Brand.ValueObjects;

public sealed class BrandName : ValueObject
{
    public string Value { get; }

    private const int MinLength = 2;
    public const int MaxLength = 100;

    private BrandName(string value)
    {
        Value = value;
    }

    public static BrandName Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام برند الزامی است.");

        var normalized = Normalize(name);
        Validate(normalized);

        return new BrandName(normalized);
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
            throw new DomainException($"نام برند باید حداقل {MinLength} کاراکتر باشد.");

        if (name.Length > MaxLength)
            throw new DomainException($"نام برند نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");
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

    public static implicit operator string(BrandName name) => name.Value;
}