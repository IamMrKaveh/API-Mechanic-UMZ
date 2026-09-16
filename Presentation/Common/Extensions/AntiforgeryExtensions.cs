using Microsoft.AspNetCore.Antiforgery;

namespace Presentation.Common.Extensions;

public static class AntiforgeryExtensions
{
    private const string HeaderName = "X-XSRF-TOKEN";
    private const string CookieName = "XSRF-TOKEN";
    private const string FormFieldName = "__RequestVerificationToken";

    public static IServiceCollection AddApplicationAntiforgery(this IServiceCollection services)
    {
        services.AddAntiforgery(options =>
        {
            options.HeaderName = HeaderName;
            options.FormFieldName = FormFieldName;
            options.Cookie.Name = CookieName;
            options.Cookie.HttpOnly = false;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.SuppressXFrameOptionsHeader = false;
        });
        return services;
    }

    public static IApplicationBuilder UseApplicationAntiforgery(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var isExcluded = path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                             || path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase)
                             || path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);

            var needsToken = !isExcluded
                             && HttpMethods.IsGet(context.Request.Method)
                             && (context.User?.Identity?.IsAuthenticated == true
                                 || IsVersionedAuthPath(path));

            if (needsToken)
            {
                var antiforgery = context.RequestServices.GetRequiredService<IAntiforgery>();
                var tokens = antiforgery.GetAndStoreTokens(context);
                if (!string.IsNullOrEmpty(tokens.RequestToken))
                {
                    context.Response.Cookies.Append(
                        CookieName,
                        tokens.RequestToken,
                        new CookieOptions
                        {
                            HttpOnly = false,
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Path = "/"
                        });
                }
            }

            await next(context);
        });
    }

    private static bool IsVersionedAuthPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return segments.Length >= 3
               && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase)
               && segments[1].StartsWith('v')
               && int.TryParse(segments[1].AsSpan(1), out _)
               && segments[2].Equals("auth", StringComparison.OrdinalIgnoreCase);
    }
}
