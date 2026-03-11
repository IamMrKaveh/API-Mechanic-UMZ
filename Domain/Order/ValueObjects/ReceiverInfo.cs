namespace Domain.Order.ValueObjects;

public sealed record ReceiverInfo
{
    public string FullName { get; }
    public string PhoneNumber { get; }

    private ReceiverInfo(string fullName, string phoneNumber)
    {
        FullName = fullName;
        PhoneNumber = phoneNumber;
    }

    public static ReceiverInfo Create(string fullName, string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Receiver full name cannot be empty.", nameof(fullName));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Receiver phone number cannot be empty.", nameof(phoneNumber));

        var normalized = NormalizePhoneNumber(phoneNumber);

        return new ReceiverInfo(fullName.Trim(), normalized);
    }

    private static string NormalizePhoneNumber(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        if (digits.Length < 10 || digits.Length > 15)
            throw new ArgumentException("Phone number must have between 10 and 15 digits.", nameof(phone));

        return digits;
    }

    public override string ToString() => $"{FullName} ({PhoneNumber})";
}