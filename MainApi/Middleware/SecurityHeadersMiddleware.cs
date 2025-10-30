namespace MainApi.Middleware;

/// <summary>
/// Middleware for adding comprehensive security headers to all HTTP responses
/// Implements OWASP security best practices
/// </summary>
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
        // Remove server information leakage
        context.Response.Headers.Remove("Server");
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("X-AspNet-Version");
        context.Response.Headers.Remove("X-AspNetMvc-Version");

        // X-Content-Type-Options: Prevent MIME type sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // X-Frame-Options: Prevent clickjacking
        context.Response.Headers["X-Frame-Options"] = _options.XFrameOptions ?? "SAMEORIGIN";

        // X-XSS-Protection: Enable browser XSS filter (legacy browsers)
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Referrer-Policy: Control referrer information
        context.Response.Headers["Referrer-Policy"] = _options.ReferrerPolicy ?? "strict-origin-when-cross-origin";

        // Permissions-Policy: Control browser features
        var permissionsPolicy = _options.PermissionsPolicy ?? "camera=(), microphone=(), geolocation=(), payment=()";
        context.Response.Headers["Permissions-Policy"] = permissionsPolicy;

        // HSTS: Enforce HTTPS (only in production)
        if (context.Request.IsHttps && !_environment.IsDevelopment())
        {
            var hstsValue = $"max-age={_options.HstsMaxAge}; includeSubDomains";
            if (_options.HstsPreload)
                hstsValue += "; preload";

            context.Response.Headers["Strict-Transport-Security"] = hstsValue;
        }

        // Content Security Policy
        var csp = BuildContentSecurityPolicy();
        if (!string.IsNullOrEmpty(csp))
        {
            context.Response.Headers["Content-Security-Policy"] = csp;
        }

        // Cross-Origin-Embedder-Policy
        if (_options.EnableCoep)
        {
            context.Response.Headers["Cross-Origin-Embedder-Policy"] = "require-corp";
        }

        // Cross-Origin-Opener-Policy
        if (_options.EnableCoop)
        {
            context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
        }

        // Cross-Origin-Resource-Policy
        if (_options.EnableCorp)
        {
            context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
        }

        await _next(context);
    }

    /// <summary>
    /// Builds Content Security Policy based on environment and configuration
    /// </summary>
    private string BuildContentSecurityPolicy()
    {
        if (_options.DisableCsp)
            return string.Empty;

        var cspDirectives = new List<string>();

        // Default source
        cspDirectives.Add($"default-src {_options.CspDefaultSrc ?? "'self'"}");

        // Script source
        var scriptSrc = _environment.IsDevelopment()
            ? "'self' 'unsafe-inline' 'unsafe-eval'"
            : "'self'";
        cspDirectives.Add($"script-src {_options.CspScriptSrc ?? scriptSrc}");

        // Style source
        var styleSrc = "'self' 'unsafe-inline' https://fonts.googleapis.com";
        cspDirectives.Add($"style-src {_options.CspStyleSrc ?? styleSrc}");

        // Image source
        cspDirectives.Add($"img-src {_options.CspImgSrc ?? "'self' data: https: blob:"}");

        // Font source
        cspDirectives.Add($"font-src {_options.CspFontSrc ?? "'self' https://fonts.gstatic.com data:"}");

        // Connect source (API calls) - اطمینان از اضافه شدن frontend domains
        var connectSrc = _options.CspConnectSrc
            ?? "'self' https://mechanic-umz.liara.run https://ledka-co.ir https://www.ledka-co.ir";
        cspDirectives.Add($"connect-src {connectSrc}");

        // Frame ancestors
        cspDirectives.Add($"frame-ancestors {_options.CspFrameAncestors ?? "'self'"}");

        // Base URI
        cspDirectives.Add("base-uri 'self'");

        // Form action
        cspDirectives.Add("form-action 'self'");

        // Upgrade insecure requests (production only)
        if (!_environment.IsDevelopment())
        {
            cspDirectives.Add("upgrade-insecure-requests");
        }

        return string.Join("; ", cspDirectives);
    }
}

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