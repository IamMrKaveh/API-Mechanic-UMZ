namespace Domain.Security.ValueObjects;

public sealed class DeviceInfo : ValueObject
{
    public string Value { get; }

    private const int MaxLength = 500;

    private DeviceInfo(string value)
    {
        Value = value;
    }

    public static DeviceInfo Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Unknown;

        var normalized = value.Trim();

        if (normalized.Length > MaxLength)
            normalized = normalized[..MaxLength];

        return new DeviceInfo(normalized);
    }

    public static DeviceInfo Unknown => new("Unknown");

    public bool IsUnknown() => Value.Equals("Unknown", StringComparison.OrdinalIgnoreCase);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public override string ToString() => Value;

    public static implicit operator string(DeviceInfo deviceInfo) => deviceInfo.Value;
}