using System.Security.Cryptography;
using Infrastructure.Chaos.Options;

namespace Presentation.Common.Middleware;

public class ChaosEngineeringMiddleware(
    RequestDelegate next,
    IOptions<ChaosOptions> options,
    ILogger<ChaosEngineeringMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ChaosOptions _options = options.Value;
    private readonly ILogger<ChaosEngineeringMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.IsEnabled || !ShouldAffect(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var latencyRoll = NextDouble();
        if (latencyRoll < _options.LatencyInjectionRate && _options.MaxLatencyMilliseconds > 0)
        {
            var delay = RandomNumberGenerator.GetInt32(0, _options.MaxLatencyMilliseconds + 1);
            _logger.LogWarning("Chaos: injecting {Delay}ms latency for {Path}.", delay, context.Request.Path);
            await Task.Delay(delay, context.RequestAborted);
        }

        var faultRoll = NextDouble();
        if (faultRoll < _options.FaultInjectionRate)
        {
            _logger.LogWarning("Chaos: injecting 503 fault for {Path}.", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsync("Chaos injected fault.");
            return;
        }

        await _next(context);
    }

    private bool ShouldAffect(PathString path)
    {
        var value = path.Value ?? string.Empty;

        foreach (var excluded in _options.ExcludedPathPrefixes)
        {
            if (value.StartsWith(excluded, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        if (_options.IncludedPathPrefixes.Length == 0)
            return true;

        foreach (var included in _options.IncludedPathPrefixes)
        {
            if (value.StartsWith(included, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static double NextDouble()
    {
        Span<byte> buffer = stackalloc byte[8];
        RandomNumberGenerator.Fill(buffer);
        var ulongValue = BitConverter.ToUInt64(buffer) >> 11;
        return ulongValue / (double)(1UL << 53);
    }
}

public static class ChaosEngineeringMiddlewareExtensions
{
    public static IApplicationBuilder UseChaosEngineering(this IApplicationBuilder builder)
        => builder.UseMiddleware<ChaosEngineeringMiddleware>();
}
