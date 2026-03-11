namespace Infrastructure.Communication.Options;

internal class KavenegarOptions
{
    public const string SectionName = "Kavenegar";

    public string ApiKey { get; set; } = string.Empty;
    public string SenderNumber { get; set; } = string.Empty;
}