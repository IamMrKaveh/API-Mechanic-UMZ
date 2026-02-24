namespace Infrastructure.Wallet.BackgroundServices;

public class WalletReservationExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WalletReservationExpiryService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public WalletReservationExpiryService(
        IServiceScopeFactory scopeFactory,
        ILogger<WalletReservationExpiryService> logger
        )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken st
        )
    {
        _logger.LogInformation("WalletReservationExpiryService started.");

        while (!st.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredReservationsAsync(st);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WalletReservationExpiryService.");
            }

            await Task.Delay(_interval, st);
        }
    }

    private async Task ProcessExpiredReservationsAsync(
        CancellationToken ct
        )
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var walletsWithExpiredReservations = await dbContext.Wallets
            .Include(w => w.Reservations)
            .Where(w => w.Reservations.Any(r =>
                r.Status == WalletReservationStatus.Pending &&
                r.ExpiresAt.HasValue &&
                r.ExpiresAt.Value < DateTime.UtcNow))
            .ToListAsync(ct);

        if (!walletsWithExpiredReservations.Any())
            return;

        foreach (var wallet in walletsWithExpiredReservations)
        {
            wallet.ExpireReservations();
        }

        await unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation(
            "WalletReservationExpiryService: Expired reservations processed for {Count} wallets.",
            walletsWithExpiredReservations.Count);
    }
}