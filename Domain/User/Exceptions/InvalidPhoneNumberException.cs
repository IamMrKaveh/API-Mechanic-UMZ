namespace Domain.User.Exceptions;

public sealed class InvalidPhoneNumberException(string phoneNumber) : DomainException($"شماره تلفن '{phoneNumber}' نامعتبر است. شماره باید با ۰۹ شروع شود و ۱۱ رقم باشد.")
{
    public string PhoneNumber { get; } = phoneNumber;

    public override string ErrorCode => "INVALID_PHONE_NUMBER";
}