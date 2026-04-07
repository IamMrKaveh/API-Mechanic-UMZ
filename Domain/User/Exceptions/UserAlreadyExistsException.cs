using Domain.Common.Exceptions;

namespace Domain.User.Exceptions;

public sealed class UserAlreadyExistsException : DomainException
{
    public string PhoneNumber { get; }

    public override string ErrorCode => "USER_ALREADY_EXISTS";

    public UserAlreadyExistsException(string phoneNumber)
        : base($"کاربری با شماره تلفن '{phoneNumber}' قبلاً ثبت‌نام کرده است.")
    {
        PhoneNumber = phoneNumber;
    }
}