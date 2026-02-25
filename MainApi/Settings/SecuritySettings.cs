namespace MainApi.Settings;

public class SecuritySettings
{
    public const string SectionName = "Security";
    public List<string> AdminIpWhitelist { get; set; } = [];
}