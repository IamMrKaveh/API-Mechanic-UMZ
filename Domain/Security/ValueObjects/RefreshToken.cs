namespace Domain.Security.ValueObjects;

public sealed class RefreshToken : ValueObject
{
    public string Value { get; }

    private const int MinLength = 32;
    private const int MaxLength = 512;

    private RefreshToken(string value)
    {
        Value = value;
    }

    public static RefreshToken Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("توکن رفرش الزامی است.");

        var normalized = value.Trim();

        if (normalized.Length < MinLength)
            throw new DomainException($"توکن رفرش باید حداقل {MinLength} کاراکتر باشد.");

        if (normalized.Length > MaxLength)
            throw new DomainException($"توکن رفرش نمی‌تواند بیش از {MaxLength} کاراکتر باشد.");

        return new RefreshToken(normalized);
    }

    public static RefreshToken Generate()
    {
        var bytes = new byte[64];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return new RefreshToken(Convert.ToBase64String(bytes));
    }

    public bool Matches(string other)
    {
        if (string.IsNullOrWhiteSpace(other))
            return false;

        return string.Equals(Value, other.Trim(), StringComparison.Ordinal);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(RefreshToken token) => token.Value;
}