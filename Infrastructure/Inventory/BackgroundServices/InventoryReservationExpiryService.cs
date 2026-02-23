namespace Infrastructure.Inventory.BackgroundServices;

/// <summary>
/// سرویس پس‌زمینه برای آزادسازی خودکار رزروهای منقضی‌شده سبد خرید
/// مشابه PaymentCleanupService - هر 5 دقیقه اجرا می‌شود
/// </summary>
public class InventoryReservationExpiryService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InventoryReservationExpiryService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    public InventoryReservationExpiryService(
        IServiceProvider serviceProvider,
        ILogger<InventoryReservationExpiryService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Inventory Reservation Expiry Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredReservationsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing expired inventory reservations.");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Inventory Reservation Expiry Service stopped.");
    }

    private async Task ProcessExpiredReservationsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Persistence.Context.DBContext>();
        var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();

        var now = DateTime.UtcNow;

        // پیدا کردن رزروهای منقضی‌شده که هنوز release نشده‌اند
        var expiredReservations = await context.InventoryTransactions
            .Where(t =>
                t.TransactionType == TransactionType.Reservation.Value &&
                !t.IsReversed &&
                t.ExpiresAt.HasValue &&
                t.ExpiresAt.Value < now)
            .GroupBy(t => new { t.VariantId, t.ReferenceNumber })
            .Select(g => new
            {
                g.Key.VariantId,
                g.Key.ReferenceNumber,
                TotalQuantity = g.Sum(t => Math.Abs(t.QuantityChange))
            })
            .ToListAsync(ct);

        if (!expiredReservations.Any()) return;

        _logger.LogInformation(
            "Found {Count} expired reservation groups to release.",
            expiredReservations.Count);

        foreach (var reservation in expiredReservations)
        {
            try
            {
                ServiceResult result;

                if (!string.IsNullOrEmpty(reservation.ReferenceNumber))
                {
                    // آزادسازی بر اساس referenceNumber (ORDER-xxx یا CART-xxx)
                    result = await inventoryService.RollbackReservationsAsync(
                        reservation.ReferenceNumber, ct);
                }
                else
                {
                    result = await inventoryService.RollbackReservationAsync(
                        reservation.VariantId,
                        reservation.TotalQuantity,
                        null,
                        "آزادسازی خودکار رزرو منقضی‌شده",
                        ct);
                }

                if (result.IsSucceed)
                {
                    _logger.LogInformation(
                        "Released expired reservation: Variant {VariantId}, Qty {Qty}, Ref {Ref}",
                        reservation.VariantId, reservation.TotalQuantity, reservation.ReferenceNumber);
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to release expired reservation for Variant {VariantId}: {Error}",
                        reservation.VariantId, result.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error releasing expired reservation for Variant {VariantId}",
                    reservation.VariantId);
            }
        }
    }
}