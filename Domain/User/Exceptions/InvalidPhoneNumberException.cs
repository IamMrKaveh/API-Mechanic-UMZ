namespace Domain.User.Exceptions;

public class InvalidPhoneNumberException : DomainException
{
    public string PhoneNumber { get; }

    public InvalidPhoneNumberException(string phoneNumber)
        : base($"شماره تلفن '{phoneNumber}' نامعتبر است. شماره باید با ۰۹ شروع شود و ۱۱ رقم باشد.")
    {
        PhoneNumber = phoneNumber;
    }
}