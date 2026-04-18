namespace Infrastructure.Communication.Options;

public sealed class SmsOptions
{
    public const string SectionName = "Sms";

    public string Provider { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string SenderNumber { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
}