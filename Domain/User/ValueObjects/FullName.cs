namespace Domain.User.ValueObjects;

public sealed class FullName : ValueObject
{
    public string FirstName { get; }
    public string LastName { get; }

    private const int MaxNameLength = 50;

    private FullName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public static FullName Create(string? firstName, string? lastName)
    {
        var normalizedFirst = string.IsNullOrWhiteSpace(firstName)
            ? string.Empty
            : (firstName);

        var normalizedLast = string.IsNullOrWhiteSpace(lastName)
            ? string.Empty
            : (lastName);

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

        if (!IsPersianOrEnglish(name))
            throw new DomainException($"{fieldName} فقط می‌تواند شامل حروف فارسی یا انگلیسی باشد.");
    }

    private static bool IsPersianOrEnglish(string text)
    {
        return System.Text.RegularExpressions.Regex.IsMatch(text, @"^[\u0600-\u06FFa-zA-Z\s]+$");
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FirstName.ToLowerInvariant();
        yield return LastName.ToLowerInvariant();
    }
}