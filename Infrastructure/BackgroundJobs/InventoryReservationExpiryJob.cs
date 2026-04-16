using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Infrastructure.BackgroundJobs;

public sealed class InventoryReservationExpiryJob(
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
                await auditService.LogErrorAsync($"Error processing expired inventory reservations: {ex.Message}", ct);
            }

            await Task.Delay(_interval, ct);
        }

        await auditService.LogInformationAsync("Inventory Reservation Expiry Service stopped.", ct);
    }

    private async Task ProcessExpiredReservationsAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();

        var expiredGroups = await GetExpiredReservationGroupsAsync(scope, ct);

        if (!expiredGroups.Any()) return;

        await auditService.LogInformationAsync(
            $"Found {expiredGroups.Count} expired reservation groups to release.", ct);

        foreach (var group in expiredGroups)
        {
            try
            {
                ServiceResult result;

                if (!string.IsNullOrEmpty(group.ReferenceNumber))
                {
                    result = await inventoryService.RollbackReservationsAsync(group.ReferenceNumber, ct);
                }
                else
                {
                    result = await inventoryService.ReleaseReservationAsync(
                        group.VariantId,
                        StockQuantity.Create(group.TotalQuantity),
                        $"EXPIRY-{group.VariantId.Value}",
                        "آزادسازی خودکار رزرو منقضی‌شده",
                        ct);
                }

                if (!result.IsSuccess)
                {
                    await auditService.LogWarningAsync(
                        $"Failed to release expired reservation for Variant {group.VariantId.Value}", ct);
                }
            }
            catch (Exception ex)
            {
                await auditService.LogErrorAsync(
                    $"Error releasing expired reservation for Variant {group.VariantId.Value}: {ex.Message}", ct);
            }
        }
    }

    private static async Task<List<(VariantId VariantId, string? ReferenceNumber, int TotalQuantity)>>
        GetExpiredReservationGroupsAsync(IServiceScope scope, CancellationToken ct)
    {
        var context = scope.ServiceProvider.GetRequiredService<Persistence.Context.DBContext>();
        var now = DateTime.UtcNow;

        var expiredEntries = await context.StockLedgerEntries
            .Where(e =>
                e.EventType == StockEventType.Reservation &&
                e.CreatedAt < now.AddMinutes(-30))
            .GroupBy(e => new { VariantIdValue = e.VariantId.Value, e.ReferenceNumber })
            .Select(g => new
            {
                g.Key.VariantIdValue,
                g.Key.ReferenceNumber,
                TotalQuantity = g.Sum(e => Math.Abs(e.QuantityDelta))
            })
            .ToListAsync(ct);

        return expiredEntries
            .Select(e => (VariantId.From(e.VariantIdValue), e.ReferenceNumber, e.TotalQuantity))
            .ToList();
    }
}