using Domain.User.Exceptions;

namespace Domain.User.ValueObjects;

public sealed class PhoneNumber : ValueObject
{
    public string Value { get; }

    private const int IranPhoneNumberLength = 11;
    private const string IranMobilePrefix = "09";

    private PhoneNumber(string value) => Value = value;

    public static PhoneNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("شماره تلفن الزامی است.");

        var normalized = Normalize(value);

        if (!IsValid(normalized))
            throw new InvalidPhoneNumberException(value);

        return new PhoneNumber(normalized);
    }

    public static Result<PhoneNumber> TryCreate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<PhoneNumber>.Failure((new Error(
                "400",
                "شماره تلفن الزامی است.")));

        var normalized = Normalize(value);

        if (!IsValid(normalized))
            return Result<PhoneNumber>.Failure((new Error("400", "فرمت شماره تلفن نامعتبر است.")));

        return Result<PhoneNumber>.Success(new PhoneNumber(normalized));
    }

    public static PhoneNumber FromNormalized(string normalizedValue)
    {
        if (!IsValid(normalizedValue))
            throw new InvalidPhoneNumberException(normalizedValue);

        return new PhoneNumber(normalizedValue);
    }

    public string GetMasked()
    {
        if (Value.Length < 7)
            return Value;

        return $"{Value[..4]}***{Value[^4..]}";
    }

    public string GetInternationalFormat()
    {
        return $"+98{Value[1..]}";
    }

    public bool Matches(string other)
    {
        if (string.IsNullOrWhiteSpace(other))
            return false;

        var normalizedOther = Normalize(other);
        return Value.Equals(normalizedOther, StringComparison.Ordinal);
    }

    private static string Normalize(string value)
    {
        var digits = new string([.. value.Where(char.IsDigit)]);

        digits = digits
            .Replace("۰", "0").Replace("۱", "1").Replace("۲", "2")
            .Replace("۳", "3").Replace("۴", "4").Replace("۵", "5")
            .Replace("۶", "6").Replace("۷", "7").Replace("۸", "8")
            .Replace("۹", "9");

        if (digits.StartsWith("98") && digits.Length == 12)
            digits = string.Concat("0", digits.AsSpan(2));

        if (digits.StartsWith("0098") && digits.Length == 14)
            digits = string.Concat("0", digits.AsSpan(4));

        if (!digits.StartsWith('0') && digits.Length == 10)
            digits = "0" + digits;

        return digits;
    }

    private static bool IsValid(string value)
    {
        return value.Length == IranPhoneNumberLength &&
               value.StartsWith(IranMobilePrefix) &&
               value.All(char.IsDigit);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(PhoneNumber phoneNumber) => phoneNumber.Value;
}