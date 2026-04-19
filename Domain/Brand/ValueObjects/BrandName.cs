namespace Domain.Brand.ValueObjects;

public sealed record BrandName
{
    public const int MaxLength = 100;
    private const int MinLength = 2;

    public string Value { get; }

    private BrandName(string value) => Value = value;

    public static BrandName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("نام برند الزامی است.");

        var trimmed = value.Trim();

        if (trimmed.Length < MinLength)
            throw new DomainException($"نام برند باید حداقل {MinLength} کاراکتر باشد.");

        if (trimmed.Length > MaxLength)
            throw new DomainException($"نام برند نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");

        return new BrandName(trimmed);
    }

    public override string ToString() => Value;
}