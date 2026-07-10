using Domain.Inventory.Enums;
using Domain.Inventory.ValueObjects;
using Domain.Variant.ValueObjects;
using Infrastructure.BackgroundJobs.Options;

namespace Infrastructure.BackgroundJobs;

public sealed class InventoryReservationExpiryJob(
    IServiceScopeFactory scopeFactory,
    IDistributedLock distributedLock,
    IOptions<ReservationExpiryOptions> options,
    IDateTimeProvider dateTimeProvider) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);
    private readonly ReservationExpiryOptions _options = options.Value;
    private static readonly TimeSpan LockExpiry = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using (var startScope = scopeFactory.CreateScope())
        {
            await startScope.ServiceProvider.GetRequiredService<IAuditService>()
                .LogInformationAsync("Inventory Reservation Expiry Service started.", ct);
        }

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await using var lockHandle = await distributedLock.AcquireAsync(
                    "jobs:inventory-reservation-expiry", LockExpiry, ct);

                if (lockHandle is not null && lockHandle.IsAcquired)
                {
                    await ProcessExpiredReservationsAsync(ct);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                using var errorScope = scopeFactory.CreateScope();
                await errorScope.ServiceProvider.GetRequiredService<IAuditService>()
                    .LogErrorAsync($"Error processing expired inventory reservations: {ex.Message}", ct);
            }

            await Task.Delay(_interval, ct);
        }

        using var stopScope = scopeFactory.CreateScope();
        await stopScope.ServiceProvider.GetRequiredService<IAuditService>()
            .LogInformationAsync("Inventory Reservation Expiry Service stopped.", ct);
    }

    private async Task ProcessExpiredReservationsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();
        var auditService = scope.ServiceProvider.GetRequiredService<IAuditService>();

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

    private async Task<List<(VariantId VariantId, string? ReferenceNumber, int TotalQuantity)>>
       GetExpiredReservationGroupsAsync(IServiceScope scope, CancellationToken ct)
    {
        var context = scope.ServiceProvider.GetRequiredService<Persistence.Context.DBContext>();
        var now = dateTimeProvider.UtcNow;
        var cutoff = now.AddMinutes(-_options.ExpiryMinutes);

        var expiredEntries = await context.StockLedgerEntries
            .Where(e =>
                e.EventType == StockEventType.Reservation &&
                e.CreatedAt < cutoff &&
                e.ReferenceNumber != null &&
                !context.StockLedgerEntries.Any(r =>
                    r.ReferenceNumber == e.ReferenceNumber &&
                    (r.EventType == StockEventType.ReservationRelease ||
                     r.EventType == StockEventType.ReservationCommit)))
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