using SharedKernel.Exceptions;

namespace Domain.User.Exceptions;

public sealed class InvalidPhoneNumberException(string phoneNumber)
    : SingleValueDomainException<string>(
        "INVALID_PHONE_NUMBER",
        phoneNumber,
        $"شماره تلفن '{phoneNumber}' نامعتبر است. شماره باید با ۰۹ شروع شود و ۱۱ رقم باشد.")
{
    public string PhoneNumber => Value;
}
