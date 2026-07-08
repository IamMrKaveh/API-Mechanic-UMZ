namespace Infrastructure.BackgroundJobs.Common;

public abstract class DistributedLockedBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger logger) : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger _logger = logger;

    protected abstract string LockKey { get; }
    protected virtual TimeSpan LockExpiry => TimeSpan.FromMinutes(5);
    protected abstract TimeSpan Interval { get; }

    protected abstract Task ExecuteInsideLockAsync(IServiceProvider services, CancellationToken ct);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var distributedLock = scope.ServiceProvider.GetRequiredService<IDistributedLock>();

                await using var handle = await distributedLock.AcquireAsync(LockKey, LockExpiry, stoppingToken);
                if (handle is not null && handle.IsAcquired)
                {
                    await ExecuteInsideLockAsync(scope.ServiceProvider, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background job {Job} failed", GetType().Name);
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}