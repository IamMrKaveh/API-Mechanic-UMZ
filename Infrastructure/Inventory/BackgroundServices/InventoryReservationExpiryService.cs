namespace Infrastructure.Inventory.BackgroundServices;

/// <summary>
/// سرویس پس‌زمینه برای آزادسازی خودکار رزروهای منقضی‌شده سبد خرید
/// مشابه PaymentCleanupService - هر 5 دقیقه اجرا می‌شود
/// </summary>
public class InventoryReservationExpiryService(
    IServiceProvider serviceProvider,
    IAuditService auditService) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await auditService.LogInformationAsync("Inventory Reservation Expiry Service started.", ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredReservationsAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                await auditService.LogErrorAsync("Error processing expired inventory reservations.", ct);
            }

            await Task.Delay(_interval, ct);
        }

        await auditService.LogInformationAsync("Inventory Reservation Expiry Service stopped.", ct);
    }

    private async Task ProcessExpiredReservationsAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<Persistence.Context.DBContext>();
        var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();

        var now = DateTime.UtcNow;

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

        await auditService.LogInformationAsync(
            $"Found {expiredReservations.Count} expired reservation groups to release.",
            ct);

        foreach (var reservation in expiredReservations)
        {
            try
            {
                ServiceResult result;

                if (!string.IsNullOrEmpty(reservation.ReferenceNumber))
                {
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

                if (result.IsSuccess)
                {
                    await auditService.LogInformationAsync(
                        "Released expired reservation: Variant {VariantId}, Qty {Qty}, Ref {Ref}",
                        reservation.VariantId, reservation.TotalQuantity, reservation.ReferenceNumber);
                }
                else
                {
                    await auditService.LogWarningAsync(
                        "Failed to release expired reservation for Variant {VariantId}: {Error}",
                        reservation.VariantId, result.Error);
                }
            }
            catch (Exception)
            {
                await auditService.LogErrorAsync(
                    "Error releasing expired reservation for Variant {VariantId}",
                    reservation.VariantId);
            }
        }
    }
}