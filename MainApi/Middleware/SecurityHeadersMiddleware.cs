namespace MainApi.Middleware;

public class SecurityHeadersMiddleware
{
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
        var path = context.Request.Path.Value?.ToLower() ?? "";

        var isPaymentCallback = path.Contains("/payment") ||
                                path.Contains("/checkout") ||
                                path.Contains("/callback") ||
                                path.Contains("/verify") ||
                                context.Request.Query.Keys.Any(k => k.ToLower() == "authority") ||
                                context.Request.Query.Keys.Any(k => k.ToLower() == "status");

        if (isPaymentCallback)
        {
            context.Response.Headers.Remove("Content-Security-Policy");
            context.Response.Headers.Remove("Content-Security-Policy-Report-Only");
            context.Response.Headers.Remove("X-Frame-Options");
            context.Response.Headers.Remove("Cross-Origin-Embedder-Policy");
            context.Response.Headers.Remove("Cross-Origin-Opener-Policy");

            await _next(context);
            return;
        }

        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("X-AspNet-Version");
        context.Response.Headers.Remove("X-AspNetMvc-Version");

        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = _options.XFrameOptions ?? "SAMEORIGIN";
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        context.Response.Headers["Referrer-Policy"] = _options.ReferrerPolicy ?? "strict-origin-when-cross-origin";

        var permissionsPolicy = _options.PermissionsPolicy ?? "camera=(), microphone=(), geolocation=(), payment=*";
        context.Response.Headers["Permissions-Policy"] = permissionsPolicy;

        if (context.Request.IsHttps && !_environment.IsDevelopment())
        {
            var hstsValue = $"max-age={_options.HstsMaxAge}; includeSubDomains";
            if (_options.HstsPreload)
                hstsValue += "; preload";

            context.Response.Headers["Strict-Transport-Security"] = hstsValue;
        }

        var csp = BuildContentSecurityPolicy();
        if (!string.IsNullOrEmpty(csp))
        {
            context.Response.Headers["Content-Security-Policy"] = csp;
        }

        if (_options.EnableCoep)
        {
            context.Response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
        }

        if (_options.EnableCoop)
        {
            context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
        }

        if (_options.EnableCorp)
        {
            context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
        }

        await _next(context);
    }

    private string BuildContentSecurityPolicy()
    {
        if (_options.DisableCsp)
            return string.Empty;

        var cspDirectives = new List<string>();
        var paymentGateways = "https://*.zarinpal. com https://*.shaparak.ir https://api.zarinpal.com https://www.zarinpal. com https://sandbox.zarinpal. com https://payment.zarinpal. com";

        cspDirectives.Add($"default-src {_options.CspDefaultSrc ?? "'self'"}");

        var scriptSrc = _environment.IsDevelopment()
            ? "'self' 'unsafe-inline' 'unsafe-eval'"
            : "'self' 'unsafe-inline' 'unsafe-eval'";

        cspDirectives.Add($"script-src {scriptSrc} {paymentGateways} https://ledka-co.ir https://www.ledka-co.ir https://*.cloudflare.com https://challenges.cloudflare. com");

        var styleSrc = "'self' 'unsafe-inline' https://fonts.googleapis. com";
        cspDirectives.Add($"style-src {_options.CspStyleSrc ?? styleSrc}");

        cspDirectives.Add($"img-src {_options.CspImgSrc ?? "'self' data: https: blob:"} {paymentGateways}");
        cspDirectives.Add($"font-src {_options.CspFontSrc ?? "'self' https://fonts.gstatic.com data:"}");

        var connectSrc = _options.CspConnectSrc
            ?? "'self' https://mechanic-umz.liara.run https://ledka-co.ir https://www.ledka-co.ir";

        cspDirectives.Add($"connect-src {connectSrc} {paymentGateways} https://*.cloudflare. com wss://*.cloudflare.com");

        cspDirectives.Add($"frame-ancestors {_options.CspFrameAncestors ?? "'self'"} {paymentGateways}");
        cspDirectives.Add($"frame-src 'self' {paymentGateways} https://*.cloudflare. com https://challenges. cloudflare.com");
        cspDirectives.Add("base-uri 'self'");
        cspDirectives.Add($"form-action 'self' {paymentGateways}");

        return string.Join("; ", cspDirectives);
    }
}