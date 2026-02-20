namespace Domain.Category.ValueObjects;

public sealed class Slug : ValueObject
{
    public string Value { get; }

    private Slug(string value)
    {
        Value = value;
    }

    public static Slug Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("نام برای تولید Slug الزامی است.");

        var slug = Normalize(name);
        Validate(slug);

        return new Slug(slug);
    }

    public static Slug FromString(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("Slug نمی‌تواند خالی باشد.");

        var normalized = slug.ToLowerInvariant().Trim();
        Validate(normalized);

        return new Slug(normalized);
    }

    private static string Normalize(string name)
    {
        return name.ToLowerInvariant()
            .Trim()
            .Replace(" ", "-")
            .Replace("‌", "-")
            .Replace("ـ", "-")
            .Replace(".", "-")
            .Replace("_", "-");
    }

    private static void Validate(string slug)
    {
        if (slug.Length > 200)
            throw new DomainException("Slug نمی‌تواند بیش از ۲۰۰ کاراکتر باشد.");

        if (slug.StartsWith("-") || slug.EndsWith("-"))
            throw new DomainException("Slug نمی‌تواند با خط تیره شروع یا پایان یابد.");

        if (slug.Contains("--"))
            throw new DomainException("Slug نمی‌تواند شامل خط تیره متوالی باشد.");
    }

    public bool Matches(string other)
    {
        if (string.IsNullOrWhiteSpace(other))
            return false;

        return Value.Equals(other.ToLowerInvariant().Trim(), StringComparison.Ordinal);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Slug slug) => slug.Value;
}