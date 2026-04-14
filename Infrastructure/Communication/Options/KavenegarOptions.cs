namespace Infrastructure.Communication.Options;

public sealed class KavenegarOptions
{
    public const string SectionName = "Kavenegar";

    public string ApiKey { get; init; } = string.Empty;
    public string Sender { get; init; } = string.Empty;
    public string OtpTemplate { get; init; } = "otp";
}