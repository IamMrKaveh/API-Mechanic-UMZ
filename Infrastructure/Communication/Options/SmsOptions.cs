namespace Infrastructure.Communication.Options;

public sealed class SmsOptions
{
    public const string SectionName = "Sms";

    [Required(AllowEmptyStrings = false)]
    public string Provider { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string SenderNumber { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string BaseUrl { get; init; } = string.Empty;
}