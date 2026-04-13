using SharedKernel.Abstractions;
using SharedKernel.Exceptions;

namespace SharedKernel.ValueObjects;

public sealed class Slug : ValueObject
{
    public string Value { get; }
    public const int MaxLength = 200;

    private static readonly Regex SlugRegex = new(@"^[a-z0-9\u0600-\u06FF]+(?:-[a-z0-9\u0600-\u06FF]+)*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private Slug(string value)
    {
        Value = value;
    }

    private Slug()
    { }

    public static Slug Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("مقدار Slug الزامی است.");

        var slug = Normalize(value);
        Validate(slug);

        return new Slug(slug);
    }

    public static Slug FromString(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new DomainException("مقدار Slug الزامی است.");

        var normalized = slug.ToLowerInvariant().Trim();
        Validate(normalized);

        return new Slug(normalized);
    }

    public static Slug GenerateFrom(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("مقدار برای تولید Slug الزامی است.");

        var slug = Normalize(displayName);
        Validate(slug);
        return new Slug(slug);
    }

    private static string Normalize(string name)
    {
        var normalized = name.ToLowerInvariant().Trim();
        normalized = normalized.Replace(" ", "-")
            .Replace("‌", "-")
            .Replace("ـ", "-")
            .Replace(".", "-")
            .Replace("_", "-");

        normalized = Regex.Replace(normalized, @"-+", "-");
        normalized = normalized.Trim('-');
        return normalized;
    }

    private static void Validate(string slug)
    {
        if (slug.Length > MaxLength)
            throw new DomainException($"مقدار Slug نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");

        if (slug.StartsWith("-") || slug.EndsWith("-"))
            throw new DomainException("مقدار Slug نمی‌تواند با خط تیره شروع یا پایان یابد.");

        if (slug.Contains("--"))
            throw new DomainException("مقدار Slug نمی‌تواند شامل خط تیره متوالی باشد.");

        if (!SlugRegex.IsMatch(slug))
            throw new DomainException($"'{slug}' دارای فرمت معتبر برای Slug نیست.");
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