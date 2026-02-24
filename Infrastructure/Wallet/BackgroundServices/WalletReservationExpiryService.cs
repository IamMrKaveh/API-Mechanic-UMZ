namespace Infrastructure.Wallet.BackgroundServices;

/// <summary>
/// Expires pending wallet reservations whose <c>ExpiresAt</c> has passed.
/// Processes reservations in small batches directly against the WalletReservation table;
/// never loads full Wallet aggregates, avoiding high memory usage and long-running locks.
/// </summary>
public class WalletReservationExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WalletReservationExpiryService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
    private const int BatchSize = 200;

    public WalletReservationExpiryService(
        IServiceScopeFactory scopeFactory,
        ILogger<WalletReservationExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken st)
    {
        _logger.LogInformation("WalletReservationExpiryService started.");

        while (!st.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(st);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WalletReservationExpiryService.");
            }

            await Task.Delay(_interval, st);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IWalletRepository>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var expired = await repository.GetExpiredReservationBatchAsync(BatchSize, ct);

        if (expired.Count == 0)
            return;

        int successCount = 0;
        int skipCount = 0;

        foreach (var reservation in expired)
        {
            try
            {
                var processed = await repository.ExpireReservationAsync(
                    reservation.ReservationId,
                    reservation.WalletId,
                    reservation.Amount,
                    ct);

                if (processed)
                {
                    successCount++;
                    if (reservation.OrderId > 0)
                    {
                        await mediator.Send(
                            new ReleaseWalletReservationCommand(reservation.UserId, reservation.OrderId),
                            ct);
                    }
                }
                else
                {
                    skipCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to expire reservation {ReservationId} for wallet {WalletId}.",
                    reservation.ReservationId, reservation.WalletId);
            }
        }

        _logger.LogInformation(
            "WalletReservationExpiryService: batch complete – expired={Success}, skipped={Skip}.",
            successCount, skipCount);
    }
}