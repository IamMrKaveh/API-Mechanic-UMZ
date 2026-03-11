namespace Infrastructure.Auth.Options;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public int OtpExpirationMinutes { get; init; } = 5;
    public int MaxOtpAttempts { get; init; } = 3;
    public int LockoutDurationMinutes { get; init; } = 15;
    public int RefreshTokenExpirationDays { get; init; } = 30;
}