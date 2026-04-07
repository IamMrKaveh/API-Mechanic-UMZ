using Domain.Common.Exceptions;

namespace Domain.User.Exceptions;

public sealed class InvalidPhoneNumberException : DomainException
{
    public string PhoneNumber { get; }

    public override string ErrorCode => "INVALID_PHONE_NUMBER";

    public InvalidPhoneNumberException(string phoneNumber)
        : base($"شماره تلفن '{phoneNumber}' نامعتبر است. شماره باید با ۰۹ شروع شود و ۱۱ رقم باشد.")
    {
        PhoneNumber = phoneNumber;
    }
}