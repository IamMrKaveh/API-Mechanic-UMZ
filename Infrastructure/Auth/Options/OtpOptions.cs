namespace Infrastructure.Auth.Options;

public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    public int Length { get; init; } = 6;
    public int ExpirationMinutes { get; init; } = 5;
    public int MaxAttempts { get; init; } = 3;
}