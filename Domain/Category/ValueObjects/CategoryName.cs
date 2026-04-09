namespace Domain.Category.ValueObjects;

public sealed class CategoryName : ValueObject
{
    private const int MinLength = 2;
    public const int MaxLength = 100;

    public string Value { get; }

    private CategoryName(string value) => Value = value;

    public static CategoryName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("نام دسته‌بندی الزامی است.");

        var trimmed = value.Trim();

        if (trimmed.Length < MinLength)
            throw new DomainException($"نام دسته‌بندی باید حداقل {MinLength} کاراکتر باشد.");

        if (trimmed.Length > MaxLength)
            throw new DomainException($"نام دسته‌بندی نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");

        return new CategoryName(trimmed);
    }

    public bool IsSameAs(CategoryName other)
        => Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;

    public static implicit operator string(CategoryName name) => name.Value;
}