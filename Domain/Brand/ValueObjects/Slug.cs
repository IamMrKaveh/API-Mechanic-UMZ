using System.Text.RegularExpressions;

namespace Domain.Brand.ValueObjects;

public sealed record Slug
{
    private static readonly Regex SlugRegex = new(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.Compiled);

    public string Value { get; }

    private Slug(string value) => Value = value;

    public static Slug Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Slug cannot be empty.", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (!SlugRegex.IsMatch(normalized))
            throw new ArgumentException($"'{value}' is not a valid slug format.", nameof(value));

        return new Slug(normalized);
    }

    public static Slug GenerateFrom(string displayName)
    {
        var slug = Regex.Replace(displayName.Trim().ToLowerInvariant(), @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = slug.Trim('-');
        return Create(slug);
    }

    public override string ToString() => Value;
}