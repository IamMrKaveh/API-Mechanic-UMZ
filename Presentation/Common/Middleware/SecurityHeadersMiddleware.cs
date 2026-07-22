using Presentation.Common.Options;

namespace Presentation.Common.Middleware;

public class SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityHeadersOptions> options)
{
    private const string NonceItemKey = "CspNonce";

    private readonly RequestDelegate _next = next;
    private readonly SecurityHeadersOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        string? nonce = null;
        if (_options.EnableNonce)
        {
            nonce = GenerateNonce();
            context.Items[NonceItemKey] = nonce;
        }

        ApplySecurityHeaders(context, nonce);
        await _next(context);
    }

    private void ApplySecurityHeaders(HttpContext context, string? nonce)
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

        if (_options.EnableCoop)
            headers.Append("Cross-Origin-Opener-Policy", "same-origin");
        if (_options.EnableCoep)
            headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
        if (_options.EnableCorp)
            headers.Append("Cross-Origin-Resource-Policy", "same-origin");

        if (_options.HstsMaxAge > 0)
        {
            var hstsValue = $"max-age={_options.HstsMaxAge}; includeSubDomains";
            if (_options.HstsPreload) hstsValue += "; preload";
            headers.Append("Strict-Transport-Security", hstsValue);
        }

        if (!_options.DisableCsp)
            ApplyCsp(headers, nonce);
    }

    private void ApplyCsp(IHeaderDictionary headers, string? nonce)
    {
        var cspParts = new List<string>();

        if (!string.IsNullOrEmpty(_options.CspDefaultSrc))
            cspParts.Add($"default-src {_options.CspDefaultSrc}");

        if (!string.IsNullOrEmpty(_options.CspScriptSrc))
        {
            var scriptSrc = _options.CspScriptSrc;
            if (!string.IsNullOrEmpty(nonce))
                scriptSrc = $"{scriptSrc} 'nonce-{nonce}'";
            cspParts.Add($"script-src {scriptSrc}");
        }

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

        cspParts.Add("base-uri 'self'");
        cspParts.Add("form-action 'self'");
        cspParts.Add("object-src 'none'");

        if (cspParts.Count > 0)
            headers.Append("Content-Security-Policy", string.Join("; ", cspParts));
    }

    private static string GenerateNonce()
    {
        var bytes = new byte[16];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
