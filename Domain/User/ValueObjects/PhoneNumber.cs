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

    public static ServiceResult<PhoneNumber> TryCreate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return ServiceResult<PhoneNumber>.Failure(new Error("PhoneNumber.Empty", "شماره تلفن الزامی است.", ErrorType.Validation));

        var normalized = Normalize(value);

        if (!IsValid(normalized))
            return ServiceResult<PhoneNumber>.Failure(new Error("PhoneNumber.InvalidFormat", "فرمت شماره تلفن نامعتبر است.", ErrorType.Validation));

        return ServiceResult<PhoneNumber>.Success(new PhoneNumber(normalized));
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

    public static implicit operator string(PhoneNumber phoneNumber) => phoneNumber.Value;
}