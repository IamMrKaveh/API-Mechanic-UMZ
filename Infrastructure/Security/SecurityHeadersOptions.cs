namespace Infrastructure.Security;

public class SecurityHeadersOptions
{
    public string? XFrameOptions { get; set; } = "SAMEORIGIN";
    public string? ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
    public string? PermissionsPolicy { get; set; }
    public int HstsMaxAge { get; set; } = 31536000;
    public bool HstsPreload { get; set; } = false;
    public bool DisableCsp { get; set; } = false;
    public string? CspDefaultSrc { get; set; }
    public string? CspScriptSrc { get; set; }
    public string? CspStyleSrc { get; set; }
    public string? CspImgSrc { get; set; }
    public string? CspFontSrc { get; set; }
    public string? CspConnectSrc { get; set; }
    public string? CspFrameAncestors { get; set; }
    public bool EnableCoep { get; set; } = false;
    public bool EnableCoop { get; set; } = false;
    public bool EnableCorp { get; set; } = false;
}