namespace Domain.Order.ValueObjects;

public sealed class ReceiverInfo : ValueObject
{
    public string FullName { get; }
    public string PhoneNumber { get; }
    public string? AlternativePhone { get; }

    private ReceiverInfo(string fullName, string phoneNumber, string? alternativePhone)
    {
        FullName = fullName;
        PhoneNumber = phoneNumber;
        AlternativePhone = alternativePhone;
    }

    public static ReceiverInfo Create(string fullName, string phoneNumber, string? alternativePhone = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new DomainException("نام گیرنده الزامی است.");

        if (fullName.Length > 100)
            throw new DomainException("نام گیرنده نباید بیش از ۱۰۰ کاراکتر باشد.");

        ValidatePhoneNumber(phoneNumber, "شماره موبایل");

        if (!string.IsNullOrWhiteSpace(alternativePhone))
        {
            ValidatePhoneNumber(alternativePhone, "شماره موبایل جایگزین");
        }

        return new ReceiverInfo(
            fullName.Trim(),
            phoneNumber.Trim(),
            alternativePhone?.Trim());
    }

    private static void ValidatePhoneNumber(string phoneNumber, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new DomainException($"{fieldName} الزامی است.");

        var normalized = NormalizePhoneNumber(phoneNumber);
        if (!System.Text.RegularExpressions.Regex.IsMatch(normalized, @"^09\d{9}$"))
            throw new DomainException($"{fieldName} نامعتبر است.");
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

        if (digits.StartsWith("98") && digits.Length == 12)
            digits = "0" + digits.Substring(2);

        if (!digits.StartsWith("0") && digits.Length == 10)
            digits = "0" + digits;

        return digits;
    }

    public string GetMaskedPhone()
    {
        if (PhoneNumber.Length < 4)
            return PhoneNumber;

        return PhoneNumber.Substring(0, 4) + "***" + PhoneNumber.Substring(PhoneNumber.Length - 4);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FullName;
        yield return PhoneNumber;
    }

    public override string ToString() => $"{FullName} - {PhoneNumber}";
}