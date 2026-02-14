namespace Domain.User.Exceptions;

public class UserAlreadyExistsException : DomainException
{
    public string PhoneNumber { get; }

    public UserAlreadyExistsException(string phoneNumber)
        : base($"کاربری با شماره تلفن '{phoneNumber}' قبلاً ثبت‌نام کرده است.")
    {
        PhoneNumber = phoneNumber;
    }
}