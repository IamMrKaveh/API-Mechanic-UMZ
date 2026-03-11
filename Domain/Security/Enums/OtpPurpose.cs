namespace Domain.Security.Enums;

public enum OtpPurpose
{
    EmailVerification = 1,
    PasswordReset = 2,
    PhoneVerification = 3,
    TwoFactorAuthentication = 4,
    Login = 5
}