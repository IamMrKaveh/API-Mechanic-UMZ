namespace MainApi.Middleware;

public class SecurityHeadersMiddleware
{
    private static readonly string[] PaymentCallbackPaths = ["/payment", "/checkout", "/callback", "/verify"];
    private static readonly string[] PaymentQueryParams = ["authority", "status"];

    private static readonly string PaymentGateways =
        "https://*.zarinpal.com https://*.shaparak.ir https://api.zarinpal.com " +
        "https://www.zarinpal.com https://sandbox.zarinpal.com https://payment.zarinpal.com";

    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly SecurityHeadersOptions _options;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        IWebHostEnvironment environment,
        IOptions<SecurityHeadersOptions> options)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsPaymentCallback(context))
        {
            RemovePaymentRestrictiveHeaders(context);
            await _next(context);
            return;
        }

        RemoveServerIdentityHeaders(context);
        ApplyStandardSecurityHeaders(context);
        ApplyHstsIfRequired(context);
        ApplyContentSecurityPolicy(context);
        ApplyCrossOriginPolicies(context);

        await _next(context);
    }

    private static bool IsPaymentCallback(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

        return PaymentCallbackPaths.Any(path.Contains) ||
               context.Request.Query.Keys.Any(k =>
                   PaymentQueryParams.Any(p => p.Equals(k, StringComparison.OrdinalIgnoreCase)));
    }

    private static void RemovePaymentRestrictiveHeaders(HttpContext context)
    {
        context.Response.Headers.Remove("Content-Security-Policy");
        context.Response.Headers.Remove("Content-Security-Policy-Report-Only");
        context.Response.Headers.Remove("X-Frame-Options");
        context.Response.Headers.Remove("Cross-Origin-Embedder-Policy");
        context.Response.Headers.Remove("Cross-Origin-Opener-Policy");
    }

    private static void RemoveServerIdentityHeaders(HttpContext context)
    {
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("X-AspNet-Version");
        context.Response.Headers.Remove("X-AspNetMvc-Version");
    }

    private void ApplyStandardSecurityHeaders(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = _options.XFrameOptions ?? "SAMEORIGIN";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = _options.ReferrerPolicy ?? "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] =
            _options.PermissionsPolicy ?? "camera=(), microphone=(), geolocation=(), payment=*";
    }

    private void ApplyHstsIfRequired(HttpContext context)
    {
        if (!context.Request.IsHttps || _environment.IsDevelopment())
            return;

        var hsts = $"max-age={_options.HstsMaxAge}; includeSubDomains";
        if (_options.HstsPreload)
            hsts += "; preload";

        context.Response.Headers["Strict-Transport-Security"] = hsts;
    }

    private void ApplyContentSecurityPolicy(HttpContext context)
    {
        var csp = BuildContentSecurityPolicy();
        if (!string.IsNullOrEmpty(csp))
            context.Response.Headers["Content-Security-Policy"] = csp;
    }

    private void ApplyCrossOriginPolicies(HttpContext context)
    {
        if (_options.EnableCoep)
            context.Response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";

        if (_options.EnableCoop)
            context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";

        if (_options.EnableCorp)
            context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
    }

    private string BuildContentSecurityPolicy()
    {
        if (_options.DisableCsp)
            return string.Empty;

        var csp = new List<string>
        {
            $"default-src {_options.CspDefaultSrc ?? "'self'"}",
            BuildScriptSrc(),
            $"style-src {_options.CspStyleSrc ?? "'self' 'unsafe-inline' https://fonts.googleapis.com"}",
            $"img-src {_options.CspImgSrc ?? "'self' data: https: blob:"} {PaymentGateways}",
            $"font-src {_options.CspFontSrc ?? "'self' https://fonts.gstatic.com data:"}",
            BuildConnectSrc(),
            $"frame-ancestors {_options.CspFrameAncestors ?? "'self'"} {PaymentGateways}",
            $"frame-src 'self' {PaymentGateways} https://*.cloudflare.com https://challenges.cloudflare.com",
            "base-uri 'self'",
            $"form-action 'self' {PaymentGateways}"
        };

        return string.Join("; ", csp);
    }

    private string BuildScriptSrc()
    {
        var scriptSrc = "'self' 'unsafe-inline' 'unsafe-eval'";
        return $"script-src {scriptSrc} {PaymentGateways} " +
               "https://ledka-co.ir https://www.ledka-co.ir " +
               "https://*.cloudflare.com https://challenges.cloudflare.com";
    }

    private string BuildConnectSrc()
    {
        var connectSrc = _options.CspConnectSrc ??
            "'self' https://mechanic-umz.liara.run https://ledka-co.ir https://www.ledka-co.ir";

        return $"connect-src {connectSrc} {PaymentGateways} " +
               "https://*.cloudflare.com wss://*.cloudflare.com";
    }
}