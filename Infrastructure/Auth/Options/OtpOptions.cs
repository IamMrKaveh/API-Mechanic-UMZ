namespace Infrastructure.Auth.Options;

public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    [Range(4, 10)]
    public int Length { get; init; } = 6;

    [Range(1, 60)]
    public int ExpirationMinutes { get; init; } = 2;

    [Range(1, 20)]
    public int MaxAttempts { get; init; } = 3;
}