using System.Net.Sockets;
using SharedKernel.Abstractions;

namespace Tests.SharedKernel.ValueObjects;

public sealed class IpAddress : ValueObject
{
    public string Value { get; }

    private IpAddress(string value) => Value = value;

    public static IpAddress Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("IP address cannot be empty.", nameof(value));

        var trimmed = value.Trim();

        if (!IsValidIp(trimmed))
            throw new ArgumentException($"'{value}' is not a valid IP address.", nameof(value));

        return new IpAddress(trimmed);
    }

    public static IpAddress Unknown => new("0.0.0.0");
    public static IpAddress System => new("127.0.0.1");

    private static bool IsValidIp(string value)
    {
        if (value.Contains(':'))
        {
            return global::System.Net.IPAddress.TryParse(value, out var parsedIp)
                   && parsedIp.AddressFamily == AddressFamily.InterNetworkV6;
        }

        var parts = value.Split('.');
        if (parts.Length != 4)
            return false;

        foreach (var part in parts)
        {
            if (part.Length == 0 || part.Length > 3)
                return false;

            if (!part.All(ch => ch is >= '0' and <= '9'))
                return false;

            if (part.Length > 1 && part[0] == '0')
                return false;

            if (!int.TryParse(part, out var octet) || octet is < 0 or > 255)
                return false;
        }

        return true;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
