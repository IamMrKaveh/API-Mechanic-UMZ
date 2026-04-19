namespace Presentation.Common.Extensions;

public static class CorsExtensions
{
    private const string CorsPolicy = "AllowClient";

    public static IServiceCollection AddCustomCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Security:AllowedOrigins")
            .Get<string[]>();

        if (allowedOrigins is null || allowedOrigins.Length == 0)
            throw new InvalidOperationException(
                "Security:AllowedOrigins در تنظیمات مقداردهی نشده است. " +
                "برای محیط‌های production باید domain‌های مجاز تعریف شوند.");

        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicy, policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });

        return services;
    }

    public static IApplicationBuilder UseCustomCors(this IApplicationBuilder app)
        => app.UseCors(CorsPolicy);
}