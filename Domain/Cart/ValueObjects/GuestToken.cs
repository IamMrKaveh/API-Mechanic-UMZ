namespace Domain.Cart.ValueObjects;

public sealed class GuestToken : ValueObject
{
    public string Value { get; }

    private GuestToken(string value) => Value = value;

    public static GuestToken Generate() => new(Guid.NewGuid().ToString("N").ToUpperInvariant());

    public static GuestToken Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Guest token cannot be empty.", nameof(value));
        if (value.Trim().Length < 8)
            throw new ArgumentException("Guest token is too short.", nameof(value));
        return new GuestToken(value.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}