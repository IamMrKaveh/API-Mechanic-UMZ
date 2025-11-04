namespace MainApi.Services.Security;

/// <summary>
/// Configuration options for Security Headers Middleware
/// </summary>
public class SecurityHeadersOptions
{
    // X-Frame-Options
    public string? XFrameOptions { get; set; } = "SAMEORIGIN";

    // Referrer-Policy
    public string? ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    // Permissions-Policy
    public string? PermissionsPolicy { get; set; }

    // HSTS Settings
    public int HstsMaxAge { get; set; } = 31536000;
    public bool HstsPreload { get; set; } = false;

    // CSP Settings
    public bool DisableCsp { get; set; } = false;
    public string? CspDefaultSrc { get; set; }
    public string? CspScriptSrc { get; set; }
    public string? CspStyleSrc { get; set; }
    public string? CspImgSrc { get; set; }
    public string? CspFontSrc { get; set; }
    public string? CspConnectSrc { get; set; }
    public string? CspFrameAncestors { get; set; }

    // CORP/COEP/COOP
    public bool EnableCoep { get; set; } = false;
    public bool EnableCoop { get; set; } = false;
    public bool EnableCorp { get; set; } = false;
}
