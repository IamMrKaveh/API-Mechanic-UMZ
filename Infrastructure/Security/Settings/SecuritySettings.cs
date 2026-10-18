namespace Infrastructure.Security.Settings;

public class SecuritySettings
{
    public const string SectionName = "Security";
    public List<string> AdminIpWhitelist { get; set; } = [];

    /// <summary>Frontend origins trusted for redirects (same list as CORS).</summary>
    public List<string> AllowedOrigins { get; set; } = [];
}