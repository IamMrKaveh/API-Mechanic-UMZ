namespace Domain.Inventory.Aggregates;

public sealed class Inventory : AggregateRoot<InventoryId>
{
    private readonly List<StockLedgerEntry> _ledgerEntries = new();

    private Inventory()
    { }

    public ProductVariantId VariantId { get; private set; } = default!;
    public int StockQuantity { get; private set; }
    public bool IsUnlimited { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int LowStockThreshold { get; private set; } = 5;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<StockLedgerEntry> LedgerEntries => _ledgerEntries.AsReadOnly();

    public int AvailableQuantity => IsUnlimited ? int.MaxValue : StockQuantity - ReservedQuantity;

    public bool IsInStock => IsUnlimited || AvailableQuantity > 0;

    public bool IsOutOfStock => !IsUnlimited && AvailableQuantity <= 0;

    public bool IsLowStock => !IsUnlimited && AvailableQuantity > 0 && AvailableQuantity <= LowStockThreshold;

    public static Inventory Create(InventoryId id, ProductVariantId variantId, int initialStock = 0, int lowStockThreshold = 5)
    {
        if (initialStock < 0)
            throw new InvalidStockQuantityException(initialStock);

        if (lowStockThreshold < 0)
            throw new DomainException("آستانه کم‌موجودی نمی‌تواند منفی باشد.");

        var inventory = new Inventory
        {
            Id = id,
            VariantId = variantId,
            StockQuantity = initialStock,
            IsUnlimited = false,
            ReservedQuantity = 0,
            LowStockThreshold = lowStockThreshold,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (initialStock > 0)
            inventory.RaiseDomainEvent(new StockIncreasedEvent(id, variantId, initialStock, initialStock));

        return inventory;
    }

    public void IncreaseStock(int quantity, string reason, int? userId = null, string? referenceNumber = null)
    {
        if (quantity <= 0)
            throw new InvalidStockQuantityException(quantity);

        StockQuantity += quantity;
        IsUnlimited = false;
        UpdatedAt = DateTime.UtcNow;

        var entry = StockLedgerEntry.StockIn(
            VariantId,
            quantity,
            StockQuantity,
            0,
            referenceNumber,
            reason,
            userId: userId);
        _ledgerEntries.Add(entry);

        RaiseDomainEvent(new StockIncreasedEvent(Id, VariantId, quantity, StockQuantity, reason));
    }

    public void DecreaseStock(int quantity, string reason, int? userId = null, string? referenceNumber = null)
    {
        if (quantity <= 0)
            throw new InvalidStockQuantityException(quantity);

        if (!IsUnlimited && StockQuantity < quantity)
            throw new InsufficientStockException(VariantId, quantity, StockQuantity);

        if (!IsUnlimited)
            StockQuantity -= quantity;

        UpdatedAt = DateTime.UtcNow;

        var entry = StockLedgerEntry.Adjustment(
            VariantId,
            -quantity,
            IsUnlimited ? 0 : StockQuantity,
            reason,
            userId);
        _ledgerEntries.Add(entry);

        RaiseDomainEvent(new StockDecreasedEvent(Id, VariantId, quantity, IsUnlimited ? -1 : StockQuantity, reason));
    }

    public void SetUnlimited()
    {
        IsUnlimited = true;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new StockSetUnlimitedEvent(Id, VariantId));
    }

    public void RemoveUnlimited(int currentStock)
    {
        if (!IsUnlimited)
            return;

        if (currentStock < 0)
            throw new InvalidStockQuantityException(currentStock);

        IsUnlimited = false;
        StockQuantity = currentStock;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ReserveStock(int quantity, string referenceNumber, int? orderItemId = null, int? userId = null, string? correlationId = null)
    {
        if (quantity <= 0)
            throw new InvalidStockQuantityException(quantity);

        if (!IsUnlimited && AvailableQuantity < quantity)
            throw new InsufficientStockException(VariantId, quantity, AvailableQuantity);

        if (!IsUnlimited)
            ReservedQuantity += quantity;

        UpdatedAt = DateTime.UtcNow;

        var entry = StockLedgerEntry.Reserve(
            VariantId,
            quantity,
            IsUnlimited ? 0 : AvailableQuantity,
            referenceNumber,
            correlationId,
            userId: userId,
            orderItemId: orderItemId);
        _ledgerEntries.Add(entry);

        RaiseDomainEvent(new StockReservedEvent(Id, VariantId, quantity, ReservedQuantity));
    }

    public void ReleaseReservation(int quantity, string referenceNumber, string? reason = null)
    {
        if (quantity <= 0)
            throw new InvalidStockQuantityException(quantity);

        if (IsUnlimited)
            return;

        var actualRelease = Math.Min(quantity, ReservedQuantity);
        if (actualRelease == 0)
            return;

        ReservedQuantity -= actualRelease;
        UpdatedAt = DateTime.UtcNow;

        var entry = StockLedgerEntry.ReleaseReservation(
            VariantId,
            actualRelease,
            AvailableQuantity,
            referenceNumber,
            reason);
        _ledgerEntries.Add(entry);

        RaiseDomainEvent(new StockReservationReleasedEvent(Id, VariantId, actualRelease, ReservedQuantity));
    }

    public void ConfirmReservation(int quantity, string referenceNumber, int? orderItemId = null)
    {
        if (quantity <= 0)
            throw new InvalidStockQuantityException(quantity);

        if (IsUnlimited)
            return;

        if (ReservedQuantity < quantity)
            throw new DomainException($"موجودی رزرو شده کافی نیست. رزرو شده: {ReservedQuantity}، درخواستی: {quantity}");

        ReservedQuantity -= quantity;
        StockQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;

        var entry = StockLedgerEntry.CommitReservation(
            VariantId,
            quantity,
            StockQuantity,
            referenceNumber,
            orderItemId);
        _ledgerEntries.Add(entry);

        RaiseDomainEvent(new StockCommittedEvent(VariantId, orderItemId ?? 0, quantity));
    }

    public void ReturnStock(int quantity, string reason, int? orderItemId = null, int? userId = null)
    {
        if (quantity <= 0)
            throw new InvalidStockQuantityException(quantity);

        if (IsUnlimited)
            return;

        StockQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;

        var entry = StockLedgerEntry.StockIn(
            VariantId,
            quantity,
            StockQuantity,
            0,
            null,
            reason,
            userId: userId);
        _ledgerEntries.Add(entry);

        RaiseDomainEvent(new StockRestoredEvent(VariantId, 0, StockQuantity, quantity, reason));
    }

    public void AdjustStock(int quantityChange, int userId, string reason)
    {
        if (IsUnlimited)
            throw new DomainException("واریانت نامحدود قابل تنظی�� دستی نیست.");

        var newStock = StockQuantity + quantityChange;
        if (newStock < 0)
            throw new NegativeStockException(VariantId, StockQuantity, Math.Abs(quantityChange));

        StockQuantity = newStock;
        UpdatedAt = DateTime.UtcNow;

        var entry = StockLedgerEntry.Adjustment(
            VariantId,
            quantityChange,
            StockQuantity,
            reason,
            userId);
        _ledgerEntries.Add(entry);

        RaiseDomainEvent(new AdjustStockEvent(VariantId, StockQuantity, quantityChange));
    }

    public void RecordDamage(int quantity, int userId, string reason)
    {
        if (quantity <= 0)
            throw new InvalidStockQuantityException(quantity);

        if (IsUnlimited)
            throw new DomainException("واریانت نامحدود قابل ثبت خسارت نیست.");

        if (StockQuantity < quantity)
            throw new InsufficientStockException(VariantId, quantity, StockQuantity);

        StockQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;

        var entry = StockLedgerEntry.Adjustment(
            VariantId,
            -quantity,
            StockQuantity,
            $"ضایعات: {reason}",
            userId);
        _ledgerEntries.Add(entry);
    }

    public void Reconcile(int calculatedStockFromTransactions, int userId)
    {
        if (IsUnlimited)
            return;

        var difference = calculatedStockFromTransactions - StockQuantity;
        if (difference == 0)
            return;

        StockQuantity = calculatedStockFromTransactions;
        UpdatedAt = DateTime.UtcNow;

        var entry = StockLedgerEntry.Adjustment(
            VariantId,
            difference,
            StockQuantity,
            $"انبارگردانی: اختلاف {difference} واحد",
            userId);
        _ledgerEntries.Add(entry);
    }

    public void SetLowStockThreshold(int threshold)
    {
        if (threshold < 0)
            throw new DomainException("آستانه کم‌موجودی نمی‌تواند منفی باشد.");

        LowStockThreshold = threshold;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanFulfill(int quantity) => IsUnlimited || AvailableQuantity >= quantity;
}