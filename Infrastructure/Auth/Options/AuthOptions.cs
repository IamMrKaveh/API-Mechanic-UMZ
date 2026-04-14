namespace Infrastructure.Auth.Options;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public int OtpExpirationMinutes { get; init; } = 5;
    public int OtpLength { get; init; } = 6;
    public int MaxFailedOtpAttempts { get; init; } = 5;
    public int OtpRateLimitWindowMinutes { get; init; } = 10;
    public int MaxOtpPerWindow { get; init; } = 3;
    public int SessionExpirationDays { get; init; } = 30;
    public int LockoutDurationMinutes { get; init; } = 15;
    public int MaxFailedLoginAttempts { get; init; } = 5;
}