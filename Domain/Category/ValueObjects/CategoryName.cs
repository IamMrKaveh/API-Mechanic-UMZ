namespace Domain.Category.ValueObjects;

public sealed class CategoryName : ValueObject
{
    public string Value { get; }

    private const int MinLength = 2;
    public const int MaxLength = 100;

    private CategoryName(string value) => Value = value;

    public static CategoryName Create(CategoryName name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام دسته‌بندی الزامی است.");

        Validate(name);

        return new CategoryName(name);
    }

    private static void Validate(CategoryName name)
    {
        if (name.Value.Length < MinLength)
            throw new DomainException($"نام دسته‌بندی باید حداقل {MinLength} کاراکتر باشد.");

        if (name.Value.Length > MaxLength)
            throw new DomainException($"نام دسته‌بندی نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");
    }

    public bool IsSameAs(CategoryName other)
    {
        if (string.IsNullOrWhiteSpace(other))
            return false;

        return Value.Equals(other.Value.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;

    public static implicit operator string(CategoryName name) => name.Value;
}