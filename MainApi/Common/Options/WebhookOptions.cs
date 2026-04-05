namespace MainApi.Common.Options;

public class WebhookOptions
{
    public const string SectionName = "Webhook";

    public List<string> AllowedPaths { get; set; } = [];
}