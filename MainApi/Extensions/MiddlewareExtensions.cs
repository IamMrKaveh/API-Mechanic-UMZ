namespace MainApi.Extensions;

public static class MiddlewareExtensions
{
    public static WebApplication UseApplicationMiddleware(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseCustomExceptionHandler();
        app.UseMiddleware<SecurityHeadersMiddleware>();

        app.UseRequestPerformanceMonitoring();

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseCustomCors();

        app.UseMiddleware<RateLimitMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAdminIpWhitelist();
        app.UseMiddleware<WebhookIpWhitelistMiddleware>();

        return app;
    }
}