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
            var needsToken = context.User?.Identity?.IsAuthenticated == true
                             && HttpMethods.IsGet(context.Request.Method)
                             && !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                             && !path.StartsWith("/metrics", StringComparison.OrdinalIgnoreCase)
                             && !path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);

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
}
