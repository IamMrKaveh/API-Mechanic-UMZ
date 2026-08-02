using System.Threading.RateLimiting;

namespace Presentation.Common.Extensions;

public static class RateLimitingExtensions
{
    public const string AdminWalletPolicy = "admin-wallet";

    public static IServiceCollection AddApplicationRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
                await context.HttpContext.Response.WriteAsync(
                    "{\"success\":false,\"message\":\"تعداد درخواست‌های ادمینی روی کیف پول از حد مجاز عبور کرده است.\"}",
                    cancellationToken);
            };

            options.AddPolicy(AdminWalletPolicy, httpContext =>
            {
                var partitionKey = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                   ?? httpContext.Connection.RemoteIpAddress?.ToString()
                                   ?? "anonymous";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    });
            });
        });

        return services;
    }

    public static IApplicationBuilder UseApplicationRateLimiter(this IApplicationBuilder app)
        => app.UseRateLimiter();
}
