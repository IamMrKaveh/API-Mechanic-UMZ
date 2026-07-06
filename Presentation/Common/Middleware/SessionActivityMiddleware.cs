using Application.Cache.Contracts;
using Application.Common.Interfaces;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;

namespace Presentation.Common.Middleware;

public sealed class SessionActivityMiddleware(
    RequestDelegate next,
    ILogger<SessionActivityMiddleware> logger,
    IServiceScopeFactory scopeFactory)
{
    private static readonly TimeSpan ThrottleWindow = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "session:activity:";

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (context.User?.Identity?.IsAuthenticated != true)
            return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUserService>();

            if (currentUser.SessionId is null || currentUser.SessionId == Guid.Empty)
                return;

            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();
            var cacheKey = $"{CacheKeyPrefix}{currentUser.SessionId.Value}";

            if (await cache.ExistsAsync(cacheKey, context.RequestAborted))
                return;

            var sessionRepository = scope.ServiceProvider.GetRequiredService<ISessionRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var sessionId = SessionId.From(currentUser.SessionId.Value);
            var session = await sessionRepository.GetByIdAsync(sessionId, context.RequestAborted);

            if (session is null || !session.IsActive)
                return;

            session.UpdateActivity(DateTime.UtcNow);
            sessionRepository.Update(session);
            await unitOfWork.SaveChangesAsync(context.RequestAborted);
            await cache.SetAsync(cacheKey, true, ThrottleWindow, context.RequestAborted);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to update session activity.");
        }
    }
}