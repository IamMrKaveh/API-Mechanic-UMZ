using Microsoft.FeatureManagement;
using SharedContracts.FeatureManagement;

namespace Application.Common.Behaviors;

public interface IIdempotentCommand
{
    Guid IdempotencyKey { get; }
}

public sealed class IdempotencyBehavior<TRequest, TResponse>(
    IIdempotencyService idempotencyService,
    IDistributedLock distributedLock,
    IFeatureManager featureManager,
    ILogger<IdempotencyBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentCommand
    where TResponse : class
{
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ContentionPollInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan ContentionMaxWait = TimeSpan.FromMilliseconds(800);

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var cached = await idempotencyService.GetResultAsync(request.IdempotencyKey, ct);
        if (cached is not null)
            return Deserialize(cached);

        var lockEnabled = await featureManager.IsEnabledAsync(
            FeatureFlags.IdempotencyDistributedLockEnabled);

        if (!lockEnabled)
        {
            logger.LogWarning(
                "The idempotency lock is disabled by the feature flag for key {Key}; the request is served without it.",
                request.IdempotencyKey);

            return await RunAsync(request, next, ct);
        }

        var lockKey = $"idempotency:{request.IdempotencyKey:N}";

        ILockHandle? lockHandle = null;
        var lockUnavailable = false;

        try
        {
            lockHandle = await distributedLock.AcquireAsync(lockKey, LockExpiry, ct);
        }
        catch (Exception ex)
        {
            // The lock guards against a duplicate that arrives while the first one
            // is still running. It is not a requirement, so a lock service that
            // is down must not take the whole request down with it: the result
            // of the key is still checked before and after the handler runs.
            lockUnavailable = true;

            logger.LogWarning(
                ex,
                "The idempotency lock for key {Key} could not be taken; the request continues without it.",
                request.IdempotencyKey);
        }

        await using (lockHandle)
        {
            // A lock that was not taken because somebody else holds it is a
            // contention. A lock that could not be taken at all is not: the
            // request is served.
            if (!lockUnavailable && (lockHandle is null || !lockHandle.IsAcquired))
            {
                var waited = TimeSpan.Zero;
                while (waited < ContentionMaxWait)
                {
                    await Task.Delay(ContentionPollInterval, ct);
                    waited += ContentionPollInterval;

                    var pollResult = await idempotencyService.GetResultAsync(request.IdempotencyKey, ct);
                    if (pollResult is not null)
                        return Deserialize(pollResult);
                }

                throw new ConcurrencyException(
                    "درخواستی با همین کلید یکتا در حال پردازش است. لطفاً چند لحظه بعد تلاش کنید.");
            }

            var recheck = await idempotencyService.GetResultAsync(request.IdempotencyKey, ct);
            if (recheck is not null)
                return Deserialize(recheck);

            return await RunAsync(request, next, ct);
        }
    }

    private async Task<TResponse> RunAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var response = await next(ct);

        await idempotencyService.MarkAsProcessedAsync(
            request.IdempotencyKey,
            System.Text.Json.JsonSerializer.Serialize(response),
            ct);

        return response;
    }

    private static TResponse Deserialize(string payload)
        => System.Text.Json.JsonSerializer.Deserialize<TResponse>(payload)!;
}
