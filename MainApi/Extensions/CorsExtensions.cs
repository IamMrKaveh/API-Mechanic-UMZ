namespace MainApi.Extensions;

public static class CorsExtensions
{
    private const string CorsPolicy = "AllowClient";

    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Security:AllowedOrigins").Get<string[]>() ??
                             new[] { "http://localhost:4200", "https://localhost:4201", "https://localhost:44318" };

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
    {
        app.UseCors(CorsPolicy);
        return app;
    }
}