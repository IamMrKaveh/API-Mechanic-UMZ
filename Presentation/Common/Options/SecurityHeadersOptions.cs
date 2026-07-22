namespace Presentation.Common.Options;

public class SecurityHeadersOptions
{
    public string? XFrameOptions { get; set; } = "SAMEORIGIN";
    public string? ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
    public string? PermissionsPolicy { get; set; } = "camera=(), microphone=(), geolocation=(), payment=*";
    public int HstsMaxAge { get; set; } = 31536000;
    public bool HstsPreload { get; set; } = true;
    public bool DisableCsp { get; set; } = false;
    public string? CspDefaultSrc { get; set; } = "'self'";
    public string? CspScriptSrc { get; set; } = "'self'";
    public string? CspStyleSrc { get; set; } = "'self' 'unsafe-inline'";
    public string? CspImgSrc { get; set; } = "'self' data: https:";
    public string? CspFontSrc { get; set; } = "'self' data:";
    public string? CspConnectSrc { get; set; } = "'self' https://api.zarinpal.com";
    public string? CspFrameAncestors { get; set; } = "'none'";
    public bool EnableCoep { get; set; } = false;
    public bool EnableCoop { get; set; } = false;
    public bool EnableCorp { get; set; } = false;
    public bool EnableNonce { get; set; } = false;
}
