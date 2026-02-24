namespace Domain.Inventory;

/// <summary>
/// Stock Ledger Entry - دفتر کل موجودی (Append-Only).
/// هر تغییر در موجودی به عنوان یک رکورد جدید ثبت می‌شود.
/// هیچ رکوردی حذف یا ویرایش نمی‌شود - فقط اضافه می‌شود.
///
/// این الگو به ما امکان می‌دهد:
/// - تاریخچه کامل موجودی را داشته باشیم
/// - موجودی در هر لحظه را محاسبه کنیم
/// - حسابرسی کامل داشته باشیم
/// - Concurrent Writes را مدیریت کنیم
/// </summary>
public sealed class StockLedgerEntry : BaseEntity, IAuditable
{
    
    public int VariantId { get; private set; }

    public int? WarehouseId { get; private set; }
    public int? OrderItemId { get; private set; }
    public int? UserId { get; private set; }

    
    public StockEventType EventType { get; private set; }

    public string EventTypeName => EventType.ToString();

    
    public int QuantityDelta { get; private set; }   

    public int BalanceAfter { get; private set; }   
    public decimal UnitCost { get; private set; }   

    
    public string? ReferenceNumber { get; private set; }  

    public string? CorrelationId { get; private set; }  
    public string? Note { get; private set; }
    public string? Source { get; private set; }  

    
    public string IdempotencyKey { get; private set; } = null!;

    
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    
    public ProductVariant? Variant { get; private set; }

    public Warehouse? Warehouse { get; private set; }

    private StockLedgerEntry()
    { }

    

    public static StockLedgerEntry StockIn(
        int variantId,
        int quantity,
        int balanceAfter,
        decimal unitCost,
        string? referenceNumber = null,
        string? note = null,
        int? warehouseId = null,
        int? userId = null)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        return Create(variantId, StockEventType.StockIn, quantity, balanceAfter,
            unitCost, referenceNumber, note, warehouseId, userId);
    }

    public static StockLedgerEntry StockOut(
        int variantId,
        int quantity,
        int balanceAfter,
        decimal unitCost,
        string? referenceNumber = null,
        string? note = null,
        int? warehouseId = null,
        int? userId = null)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        return Create(variantId, StockEventType.Sale, -quantity, balanceAfter,
            unitCost, referenceNumber, note, warehouseId, userId);
    }

    public static StockLedgerEntry Reserve(
        int variantId,
        int quantity,
        int balanceAfter,
        string referenceNumber,
        string? correlationId = null,
        int? warehouseId = null,
        int? userId = null,
        int? orderItemId = null)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        var entry = Create(variantId, StockEventType.Reservation, -quantity, balanceAfter,
            0, referenceNumber, null, warehouseId, userId);
        entry.CorrelationId = correlationId;
        entry.OrderItemId = orderItemId;
        return entry;
    }

    public static StockLedgerEntry ReleaseReservation(
        int variantId,
        int quantity,
        int balanceAfter,
        string referenceNumber,
        string? reason = null,
        int? warehouseId = null)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        return Create(variantId, StockEventType.ReservationRelease, quantity, balanceAfter,
            0, referenceNumber, reason, warehouseId);
    }

    public static StockLedgerEntry CommitReservation(
        int variantId,
        int quantity,
        int balanceAfter,
        string referenceNumber,
        int? orderItemId = null,
        int? warehouseId = null)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        var entry = Create(variantId, StockEventType.ReservationCommit, -quantity, balanceAfter,
            0, referenceNumber, null, warehouseId);
        entry.OrderItemId = orderItemId;
        return entry;
    }

    public static StockLedgerEntry Adjustment(
        int variantId,
        int delta,
        int balanceAfter,
        string reason,
        int? userId = null,
        int? warehouseId = null)
    {
        return Create(variantId, StockEventType.Adjustment, delta, balanceAfter,
            0, null, reason, warehouseId, userId);
    }

    public static StockLedgerEntry Return(
        int variantId,
        int quantity,
        int balanceAfter,
        string referenceNumber,
        string? note = null,
        int? warehouseId = null)
    {
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        return Create(variantId, StockEventType.Return, quantity, balanceAfter,
            0, referenceNumber, note, warehouseId);
    }

    

    private static StockLedgerEntry Create(
        int variantId,
        StockEventType eventType,
        int quantityDelta,
        int balanceAfter,
        decimal unitCost,
        string? referenceNumber,
        string? note,
        int? warehouseId = null,
        int? userId = null)
    {
        if (balanceAfter < 0)
            throw new DomainException("موجودی پس از این رویداد نمی‌تواند منفی باشد.");

        return new StockLedgerEntry
        {
            VariantId = variantId,
            WarehouseId = warehouseId,
            UserId = userId,
            EventType = eventType,
            QuantityDelta = quantityDelta,
            BalanceAfter = balanceAfter,
            UnitCost = unitCost,
            ReferenceNumber = referenceNumber,
            Note = note,
            Source = "System",
            IdempotencyKey = $"{variantId}:{eventType}:{referenceNumber ?? Guid.NewGuid().ToString("N")}",
            CreatedAt = DateTime.UtcNow
        };
    }
}