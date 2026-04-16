using Domain.Inventory.Entities;
using Infrastructure.Inventory.QueryServices;

namespace Infrastructure.Inventory.Services;

/// <summary>
/// سرویس دفتر کل موجودی (Stock Ledger).
/// تمام تغییرات موجودی را به صورت Append-Only ثبت می‌کند.
/// </summary>
public sealed class StockLedgerService(
    DBContext context,
    IAuditService auditService) : IStockLedgerService
{
    public async Task RecordStockInAsync(
        int variantId,
        int quantity,
        decimal unitCost,
        string? referenceNumber = null,
        string? note = null,
        int? warehouseId = null,
        int? userId = null,
        CancellationToken ct = default)
    {
        var balance = await new StockLedgerQueryService(context)
            .GetCurrentBalanceAsync(variantId, warehouseId, ct);
        var entry = StockLedgerEntry.StockIn(
            variantId, quantity, balance + quantity,
            unitCost, referenceNumber, note, warehouseId, userId);

        await AppendEntryAsync(entry, ct);

        _logger.LogInformation(
            "[StockLedger] StockIn: Variant={VariantId}, Qty={Qty}, NewBalance={Balance}",
            variantId, quantity, balance + quantity);
    }

    public async Task RecordReservationAsync(
        int variantId,
        int quantity,
        string referenceNumber,
        string? correlationId = null,
        int? warehouseId = null,
        int? userId = null,
        int? orderItemId = null,
        CancellationToken ct = default)
    {
        var balance = await new StockLedgerQueryService(context)
            .GetCurrentBalanceAsync(variantId, warehouseId, ct);

        if (balance < quantity)
            throw new DomainException($"موجودی کافی نیست. موجودی فعلی: {balance}, درخواست: {quantity}");

        var entry = StockLedgerEntry.Reserve(
            variantId, quantity, balance - quantity,
            referenceNumber, correlationId, warehouseId, userId, orderItemId);

        await AppendEntryAsync(entry, ct);
    }

    public async Task RecordReservationReleaseAsync(
        int variantId,
        int quantity,
        string referenceNumber,
        string? reason = null,
        int? warehouseId = null,
        CancellationToken ct = default)
    {
        var balance = await new StockLedgerQueryService(context)
            .GetCurrentBalanceAsync(variantId, warehouseId, ct);
        var entry = StockLedgerEntry.ReleaseReservation(
            variantId, quantity, balance + quantity,
            referenceNumber, reason, warehouseId);

        await AppendEntryAsync(entry, ct);
    }

    public async Task RecordReservationCommitAsync(
        int variantId,
        int quantity,
        string referenceNumber,
        int? orderItemId = null,
        int? warehouseId = null,
        CancellationToken ct = default)
    {
        var balance = await new StockLedgerQueryService(context)
            .GetCurrentBalanceAsync(variantId, warehouseId, ct);
        var entry = StockLedgerEntry.CommitReservation(
            variantId, quantity, balance, referenceNumber, orderItemId, warehouseId);

        await AppendEntryAsync(entry, ct);
    }

    public async Task RecordAdjustmentAsync(
        int variantId,
        int delta,
        string reason,
        int? userId = null,
        int? warehouseId = null,
        CancellationToken ct = default)
    {
        var balance = await new StockLedgerQueryService(context)
            .GetCurrentBalanceAsync(variantId, warehouseId, ct);
        var newBalance = balance + delta;

        if (newBalance < 0)
            throw new DomainException($"تنظیم موجودی باعث موجودی منفی می‌شود. موجودی فعلی: {balance}, تغییر: {delta}");

        var entry = StockLedgerEntry.Adjustment(variantId, delta, newBalance, reason, userId, warehouseId);
        await AppendEntryAsync(entry, ct);
    }

    /// <summary>
    /// تطبیق موجودی فعلی با موجودی فیزیکی (Physical Count).
    /// </summary>
    public async Task ReconcileAsync(
        int variantId,
        int physicalCount,
        string reason,
        int userId,
        int? warehouseId = null,
        CancellationToken ct = default)
    {
        var systemBalance = await new StockLedgerQueryService(context)
            .GetCurrentBalanceAsync(variantId, warehouseId, ct);

        var delta = physicalCount - systemBalance;

        if (delta == 0)
        {
            await auditService.LogInformationAsync(
                $"[StockLedger] Reconcile: Variant={variantId} - No discrepancy.",
                ct);
            return;
        }

        await auditService.LogWarningAsync(
            $"[StockLedger] Reconcile discrepancy: Variant={variantId}, System={systemBalance}, Physical={physicalCount}, Delta={delta}",
            ct);

        await RecordAdjustmentAsync(
            variantId,
            delta,
            $"[Reconcile] {reason} | System:{systemBalance}, Physical:{physicalCount}",
            userId,
            warehouseId,
            ct);
    }

    private async Task AppendEntryAsync(
        StockLedgerEntry entry,
        CancellationToken ct)
    {
        await context.StockLedgerEntries.AddAsync(entry, ct);
        await context.SaveChangesAsync(ct);
    }
}