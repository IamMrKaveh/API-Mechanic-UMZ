using Application.Inventory.Contracts;
using Domain.Inventory.Entities;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using Infrastructure.Inventory.QueryServices;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Inventory.Services;

public sealed class StockLedgerService(
    DBContext context,
    IAuditService auditService) : IStockLedgerService
{
    public async Task ReconcileAsync(VariantId variantId, UserId userId, CancellationToken ct = default)
    {
        var queryService = new StockLedgerQueryService(context);

        var lastEntry = await context.StockLedgerEntries
            .Where(e => e.VariantId == variantId)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync(ct);

        var systemBalance = lastEntry?.BalanceAfter ?? 0;

        await auditService.LogInformationAsync(
            $"[StockLedger] Reconcile check: Variant={variantId.Value}, SystemBalance={systemBalance}", ct);

        var entry = StockLedgerEntry.Adjustment(
            variantId, 0, systemBalance, "[Reconcile] بررسی انبارگردانی", userId);

        await context.StockLedgerEntries.AddAsync(entry, ct);
        await context.SaveChangesAsync(ct);
    }
}