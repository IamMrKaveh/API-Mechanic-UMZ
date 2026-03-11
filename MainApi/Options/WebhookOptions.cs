namespace MainApi.Options;

public class WebhookOptions
{
    public const string SectionName = "Webhook";

    public List<string> AllowedPaths { get; set; } = [];
}