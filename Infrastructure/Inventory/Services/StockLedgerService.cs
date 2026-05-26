using Application.Inventory.Contracts;
using Domain.Common.Interfaces;
using Domain.Inventory.Entities;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Inventory.Services;

public sealed class StockLedgerService(
    DBContext context,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IStockLedgerService
{
    public async Task ReconcileAsync(VariantId variantId, UserId userId, CancellationToken ct = default)
    {
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
        await unitOfWork.SaveChangesAsync(ct);
    }
}