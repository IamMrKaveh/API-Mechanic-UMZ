using Presentation.Common.Options;

namespace Presentation.Common.Middleware;

public class SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityHeadersOptions> options)
{
    private readonly RequestDelegate _next = next;
    private readonly SecurityHeadersOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        ApplySecurityHeaders(context);
        await _next(context);
    }

    private void ApplySecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        if (!string.IsNullOrEmpty(_options.XFrameOptions))
            headers.Append("X-Frame-Options", _options.XFrameOptions);

        if (!string.IsNullOrEmpty(_options.ReferrerPolicy))
            headers.Append("Referrer-Policy", _options.ReferrerPolicy);

        if (!string.IsNullOrEmpty(_options.PermissionsPolicy))
            headers.Append("Permissions-Policy", _options.PermissionsPolicy);

        headers.Append("X-Content-Type-Options", "nosniff");
        headers.Append("X-XSS-Protection", "1; mode=block");

        if (_options.HstsMaxAge > 0)
        {
            var hstsValue = $"max-age={_options.HstsMaxAge}";
            if (_options.HstsPreload) hstsValue += "; preload";
            headers.Append("Strict-Transport-Security", hstsValue);
        }

        if (!_options.DisableCsp)
            ApplyCsp(headers);
    }

    private void ApplyCsp(IHeaderDictionary headers)
    {
        var cspParts = new List<string>();

        if (!string.IsNullOrEmpty(_options.CspDefaultSrc))
            cspParts.Add($"default-src {_options.CspDefaultSrc}");
        if (!string.IsNullOrEmpty(_options.CspScriptSrc))
            cspParts.Add($"script-src {_options.CspScriptSrc}");
        if (!string.IsNullOrEmpty(_options.CspStyleSrc))
            cspParts.Add($"style-src {_options.CspStyleSrc}");
        if (!string.IsNullOrEmpty(_options.CspImgSrc))
            cspParts.Add($"img-src {_options.CspImgSrc}");
        if (!string.IsNullOrEmpty(_options.CspFontSrc))
            cspParts.Add($"font-src {_options.CspFontSrc}");
        if (!string.IsNullOrEmpty(_options.CspConnectSrc))
            cspParts.Add($"connect-src {_options.CspConnectSrc}");
        if (!string.IsNullOrEmpty(_options.CspFrameAncestors))
            cspParts.Add($"frame-ancestors {_options.CspFrameAncestors}");

        if (cspParts.Count > 0)
            headers.Append("Content-Security-Policy", string.Join("; ", cspParts));
    }
}