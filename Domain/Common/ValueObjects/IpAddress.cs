namespace Domain.Common.ValueObjects;

public sealed class IpAddress : ValueObject
{
    public string Value { get; }

    private IpAddress(string value) => Value = value;

    public static IpAddress Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("IP address cannot be empty.", nameof(value));

        if (!System.Net.IPAddress.TryParse(value.Trim(), out _))
            throw new ArgumentException($"'{value}' is not a valid IP address.", nameof(value));

        return new IpAddress(value.Trim());
    }

    public static IpAddress Unknown => new("0.0.0.0");

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}