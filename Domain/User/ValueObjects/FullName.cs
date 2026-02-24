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
        var normalizedFirst = NormalizePersian(firstName ?? string.Empty);
        var normalizedLast = NormalizePersian(lastName ?? string.Empty);

        ValidateName(normalizedFirst, "نام");
        ValidateName(normalizedLast, "نام خانوادگی");

        return new FullName(normalizedFirst, normalizedLast);
    }

    public static FullName Empty() => new(string.Empty, string.Empty);

    public string GetFullName()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(FirstName))
            parts.Add(FirstName);

        if (!string.IsNullOrWhiteSpace(LastName))
            parts.Add(LastName);

        return string.Join(" ", parts);
    }

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(FirstName) && !string.IsNullOrEmpty(LastName))
            return $"{FirstName} {LastName}";

        if (!string.IsNullOrEmpty(FirstName))
            return FirstName;

        if (!string.IsNullOrEmpty(LastName))
            return LastName;

        return "کاربر";
    }

    public string GetInitials()
    {
        var initials = string.Empty;

        if (!string.IsNullOrWhiteSpace(FirstName))
            initials += FirstName[0];

        if (!string.IsNullOrWhiteSpace(LastName))
            initials += LastName[0];

        return initials;
    }

    public bool IsEmpty() => string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName);

    public bool IsComplete() => !string.IsNullOrWhiteSpace(FirstName) && !string.IsNullOrWhiteSpace(LastName);

    public FullName WithFirstName(string firstName)
    {
        return Create(firstName, LastName);
    }

    public FullName WithLastName(string lastName)
    {
        return Create(FirstName, lastName);
    }

    private static string NormalizePersian(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.Trim()
            .Replace("ي", "ی")
            .Replace("ك", "ک")
            .Replace("ى", "ی");
    }

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

    public override string ToString() => GetFullName();
}