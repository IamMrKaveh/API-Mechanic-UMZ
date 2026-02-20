namespace Infrastructure.Inventory.Services;

/// <summary>
/// سرویس دفتر کل موجودی (Stock Ledger).
/// تمام تغییرات موجودی را به صورت Append-Only ثبت می‌کند.
/// </summary>
public sealed class StockLedgerService : IStockLedgerService
{
    private readonly LedkaContext _context;
    private readonly ILogger<StockLedgerService> _logger;

    public StockLedgerService(LedkaContext context, ILogger<StockLedgerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ─── ثبت رویدادهای موجودی ─────────────────────────────────────────────

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
        var balance = await new StockLedgerQueryService(_context, _logger)
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
        var balance = await new StockLedgerQueryService(_context, _logger)
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
        var balance = await new StockLedgerQueryService(_context, _logger)
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
        var balance = await new StockLedgerQueryService(_context, _logger)
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
        var balance = await new StockLedgerQueryService(_context, _logger)
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
        var systemBalance = await new StockLedgerQueryService(_context, _logger)
            .GetCurrentBalanceAsync(variantId, warehouseId, ct);
        var delta = physicalCount - systemBalance;

        if (delta == 0)
        {
            _logger.LogInformation(
                "[StockLedger] Reconcile: Variant={VariantId} - No discrepancy.", variantId);
            return;
        }

        _logger.LogWarning(
            "[StockLedger] ⚠ Reconcile discrepancy: Variant={VariantId}, System={System}, Physical={Physical}, Delta={Delta}",
            variantId, systemBalance, physicalCount, delta);

        await RecordAdjustmentAsync(variantId, delta,
            $"[Reconcile] {reason} | System:{systemBalance}, Physical:{physicalCount}",
            userId, warehouseId, ct);
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────

    private async Task AppendEntryAsync(StockLedgerEntry entry, CancellationToken ct)
    {
        await _context.StockLedgerEntries.AddAsync(entry, ct);
        await _context.SaveChangesAsync(ct);
    }

    public Task<int> GetCurrentBalanceAsync(int variantId, int? warehouseId = null, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<StockLedgerEntry>> GetLedgerAsync(int variantId, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 50, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}