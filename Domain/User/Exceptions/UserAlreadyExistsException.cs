namespace Domain.User.Exceptions;

public class UserAlreadyExistsException(string phoneNumber) : DomainException($"کاربری با شماره تلفن '{phoneNumber}' قبلاً ثبت‌نام کرده است.")
{
    public string PhoneNumber { get; } = phoneNumber;
}