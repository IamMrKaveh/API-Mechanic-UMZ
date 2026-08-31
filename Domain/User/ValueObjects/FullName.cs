namespace Domain.User.ValueObjects;

public sealed class FullName : ValueObject
{
    public string FirstName { get; }
    public string LastName { get; }

    private const int MaxNameLength = 50;

    private static readonly System.Text.RegularExpressions.Regex NameRegex =
        new(@"^[\u0600-\u06FF\u200C\u200Fa-zA-Z\s\-'.]+$",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    private FullName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public static FullName Create(string? firstName, string? lastName)
    {
        var normalizedFirst = string.IsNullOrWhiteSpace(firstName)
            ? string.Empty
            : firstName.Trim();

        var normalizedLast = string.IsNullOrWhiteSpace(lastName)
            ? string.Empty
            : lastName.Trim();

        ValidateName(normalizedFirst, "نام");
        ValidateName(normalizedLast, "نام خانوادگی");

        return new FullName(normalizedFirst, normalizedLast);
    }

    public static FullName Empty() => new(string.Empty, string.Empty);

    private static void ValidateName(string name, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        if (name.Length > MaxNameLength)
            throw new DomainException($"{fieldName} نباید بیش از {MaxNameLength} کاراکتر باشد.");

        if (!NameRegex.IsMatch(name))
            throw new DomainException($"{fieldName} فقط می‌تواند شامل حروف فارسی یا انگلیسی باشد.");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FirstName.ToLowerInvariant();
        yield return LastName.ToLowerInvariant();
    }
}
