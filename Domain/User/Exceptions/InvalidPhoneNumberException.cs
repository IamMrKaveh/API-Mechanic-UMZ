namespace Domain.User.Exceptions;

public class InvalidPhoneNumberException(string phoneNumber) : DomainException($"شماره تلفن '{phoneNumber}' نامعتبر است. شماره باید با ۰۹ شروع شود و ۱۱ رقم باشد.")
{
    public string PhoneNumber { get; } = phoneNumber;
}