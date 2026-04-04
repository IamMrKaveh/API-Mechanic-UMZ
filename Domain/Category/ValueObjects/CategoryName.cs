namespace Domain.Category.ValueObjects;

public sealed class CategoryName : ValueObject
{
    public string Value { get; }

    private const int MinLength = 2;
    public const int MaxLength = 100;

    private CategoryName(string value)
    {
        Value = value;
    }

    public static CategoryName Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام دسته‌بندی الزامی است.");

        //moved to application
        //var normalized = PersianTextNormalizer.Normalize(name);
        Validate(name);

        return new CategoryName(name);
    }

    private static void Validate(string name)
    {
        if (name.Length < MinLength)
            throw new DomainException($"نام دسته‌بندی باید حداقل {MinLength} کاراکتر باشد.");

        if (name.Length > MaxLength)
            throw new DomainException($"نام دسته‌بندی نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");
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

    public static implicit operator string(CategoryName name) => name.Value;
}