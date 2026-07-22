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
    IFeatureManager featureManager) : IPipelineBehavior<TRequest, TResponse>
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
            return System.Text.Json.JsonSerializer.Deserialize<TResponse>(cached)!;

        var lockEnabled = await featureManager.IsEnabledAsync(
            FeatureFlags.IdempotencyDistributedLockEnabled);

        if (!lockEnabled)
        {
            var response = await next(ct);
            await idempotencyService.MarkAsProcessedAsync(
                request.IdempotencyKey,
                System.Text.Json.JsonSerializer.Serialize(response),
                ct);
            return response;
        }

        var lockKey = $"idempotency:{request.IdempotencyKey:N}";

        await using var lockHandle = await distributedLock.AcquireAsync(lockKey, LockExpiry, ct);
        if (lockHandle is null || !lockHandle.IsAcquired)
        {
            var waited = TimeSpan.Zero;
            while (waited < ContentionMaxWait)
            {
                await Task.Delay(ContentionPollInterval, ct);
                waited += ContentionPollInterval;

                var pollResult = await idempotencyService.GetResultAsync(request.IdempotencyKey, ct);
                if (pollResult is not null)
                    return System.Text.Json.JsonSerializer.Deserialize<TResponse>(pollResult)!;
            }

            throw new ConcurrencyException(
                "درخواستی با همین کلید یکتا در حال پردازش است. لطفاً چند لحظه بعد تلاش کنید.");
        }

        var recheck = await idempotencyService.GetResultAsync(request.IdempotencyKey, ct);
        if (recheck is not null)
            return System.Text.Json.JsonSerializer.Deserialize<TResponse>(recheck)!;

        try
        {
            var response = await next(ct);
            await idempotencyService.MarkAsProcessedAsync(
                request.IdempotencyKey,
                System.Text.Json.JsonSerializer.Serialize(response),
                ct);
            return response;
        }
        catch
        {
            throw;
        }
    }
}
