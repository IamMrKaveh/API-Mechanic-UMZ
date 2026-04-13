using Domain.Inventory.Entities;
using Domain.Inventory.Events;
using Domain.Inventory.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Aggregates;

public sealed class Inventory : AggregateRoot<InventoryId>
{
    private readonly List<StockLedgerEntry> _ledgerEntries = new();

    private Inventory()
    { }

    public VariantId VariantId { get; private set; } = default!;
    public StockQuantity StockQuantity { get; private set; }
    public bool IsUnlimited { get; private set; }
    public StockQuantity ReservedQuantity { get; private set; }
    public int LowStockThreshold { get; private set; } = 5;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyCollection<StockLedgerEntry> LedgerEntries => _ledgerEntries.AsReadOnly();

    public int AvailableQuantity => IsUnlimited ? int.MaxValue : StockQuantity - ReservedQuantity;

    public bool IsInStock => IsUnlimited || AvailableQuantity > 0;
    public bool IsOutOfStock => !IsUnlimited && AvailableQuantity <= 0;

    public bool IsLowStock =>
        !IsUnlimited && AvailableQuantity > 0 && AvailableQuantity <= LowStockThreshold;

    public static Inventory Create(
        InventoryId id,
        VariantId variantId,
        int initialStock = 0,
        int lowStockThreshold = 5)
    {
        var stock = StockQuantity.Create(initialStock);

        if (lowStockThreshold < 0)
            throw new ArgumentOutOfRangeException(nameof(lowStockThreshold));

        var inventory = new Inventory
        {
            Id = id,
            VariantId = variantId,
            StockQuantity = stock,
            IsUnlimited = false,
            ReservedQuantity = StockQuantity.Create(0),
            LowStockThreshold = lowStockThreshold,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (initialStock > 0)
            inventory.RaiseDomainEvent(
                new StockIncreasedEvent(id, variantId, initialStock, initialStock));

        return inventory;
    }

    public Result IncreaseStock(
        int quantity,
        string reason,
        UserId? userId = null,
        string? referenceNumber = null)
    {
        if (quantity <= 0) return Result.Failure(new Error("Inventory.InvalidQuantity", "مقدار باید بزرگتر از صفر باشد."));

        var currentStock = ValueObjects.StockQuantity.Create(StockQuantity);
        StockQuantity = currentStock.Add(quantity);

        IsUnlimited = false;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var entry = StockLedgerEntry.StockIn(
            VariantId, quantity, StockQuantity, 0, referenceNumber, reason, userId: userId);

        _ledgerEntries.Add(entry);
        RaiseDomainEvent(new StockIncreasedEvent(Id, VariantId, quantity, StockQuantity, reason));

        return Result.Success();
    }

    public Result DecreaseStock(
        int quantity,
        string reason,
        UserId? userId = null,
        string? referenceNumber = null)
    {
        if (quantity <= 0) return Result.Failure(new Error("Inventory.InvalidQuantity", "مقدار باید بزرگتر از صفر باشد."));

        if (!IsUnlimited)
        {
            var currentStock = ValueObjects.StockQuantity.Create(StockQuantity);
            var subtractResult = currentStock.TrySubtract(quantity);
            if (subtractResult.IsFailure) return Result.Failure(subtractResult.Error);

            StockQuantity = subtractResult.Value;
        }

        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var entry = StockLedgerEntry.Adjustment(
            VariantId, -quantity, IsUnlimited ? 0 : StockQuantity, reason, userId);

        _ledgerEntries.Add(entry);
        RaiseDomainEvent(
            new StockDecreasedEvent(Id, VariantId, quantity, IsUnlimited ? -1 : StockQuantity, reason));

        return Result.Success();
    }

    public void SetUnlimited()
    {
        IsUnlimited = true;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
        RaiseDomainEvent(new StockSetUnlimitedEvent(Id, VariantId));
    }

    public Result RemoveUnlimited(StockQuantity currentStock)
    {
        if (!IsUnlimited) return Result.Success();

        if (currentStock < 0) return Result.Failure(new Error("Inventory.InvalidQuantity", "موجودی نمی‌تواند منفی باشد."));

        IsUnlimited = false;
        StockQuantity = currentStock;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        return Result.Success();
    }

    public Result ReserveStock(
        StockQuantity quantity,
        string referenceNumber,
        OrderItemId? orderItemId = null,
        UserId? userId = null,
        string? correlationId = null)
    {
        if (quantity <= 0) return Result.Failure(new Error("Inventory.InvalidQuantity", "مقدار باید بزرگتر از صفر باشد."));

        if (!IsUnlimited)
        {
            var available = ValueObjects.StockQuantity.Create(AvailableQuantity);
            var reserveResult = available.TrySubtract(quantity);

            if (reserveResult.IsFailure) return Result.Failure(reserveResult.Error);

            ReservedQuantity = ReservedQuantity.Add(quantity);
        }

        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var entry = StockLedgerEntry.Reserve(
            VariantId, quantity, IsUnlimited ? 0 : AvailableQuantity,
            referenceNumber, correlationId, userId: userId, orderItemId: orderItemId);

        _ledgerEntries.Add(entry);
        RaiseDomainEvent(new StockReservedEvent(Id, VariantId, quantity, ReservedQuantity));

        return Result.Success();
    }

    public Result ReleaseReservation(StockQuantity quantity, string referenceNumber, string? reason = null)
    {
        if (quantity <= 0)
            return Result.Failure(new Error("Inventory.InvalidQuantity", "مقدار باید بزرگتر از صفر باشد."));
        if (IsUnlimited)
            return Result.Success();

        var actualRelease = Math.Min(quantity, ReservedQuantity);
        if (actualRelease == 0)
            return Result.Success();

        ReservedQuantity = ReservedQuantity.Subtract(actualRelease);
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var entry = StockLedgerEntry.ReleaseReservation(
            VariantId, actualRelease, AvailableQuantity, referenceNumber, reason);

        _ledgerEntries.Add(entry);
        RaiseDomainEvent(
            new StockReservationReleasedEvent(Id, VariantId, actualRelease, ReservedQuantity));

        return Result.Success();
    }

    public Result ConfirmReservation(
        StockQuantity quantity,
        string referenceNumber,
        OrderItemId? orderItemId = null)
    {
        if (quantity <= 0) return Result.Failure(new Error("Inventory.InvalidQuantity", "مقدار باید بزرگتر از صفر باشد."));
        if (IsUnlimited) return Result.Success();

        if (ReservedQuantity < quantity)
            return Result.Failure(new Error("Inventory.InsufficientReservation", $"موجودی رزرو شده کافی نیست. رزرو شده: {ReservedQuantity}، درخواستی: {quantity}"));

        ReservedQuantity = ReservedQuantity.Subtract(quantity.Value);
        StockQuantity = StockQuantity.Subtract(quantity.Value);

        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var entry = StockLedgerEntry.CommitReservation(
            VariantId, quantity, StockQuantity, referenceNumber, orderItemId);

        _ledgerEntries.Add(entry);
        RaiseDomainEvent(new StockCommittedEvent(Id, VariantId, OrderItemId.NewId(), quantity));

        return Result.Success();
    }

    public Result ReverseStockChange(
        string idempotencyKey,
        string reason,
        UserId userId)
    {
        if (IsUnlimited) return Result.Failure(new Error("Inventory.NotApplicable", "امکان برگشت تراکنش برای واریانت نامحدود وجود ندارد."));

        var originalEntry = _ledgerEntries.FirstOrDefault(e => e.IdempotencyKey == idempotencyKey);
        if (originalEntry == null) return Result.Failure(new Error("Inventory.NotFound", "تراکنش مورد نظر یافت نشد."));

        var reversalDelta = -originalEntry.QuantityDelta;
        var currentStock = ValueObjects.StockQuantity.Create(StockQuantity);

        if (reversalDelta < 0)
        {
            var subtractResult = currentStock.TrySubtract(Math.Abs(reversalDelta));
            if (subtractResult.IsFailure) return Result.Failure(subtractResult.Error);
            StockQuantity = subtractResult.Value;
        }
        else
        {
            StockQuantity = currentStock.Add(reversalDelta);
        }

        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var entry = StockLedgerEntry.Adjustment(
            VariantId, reversalDelta, StockQuantity,
            $"برگشت تراکنش: {reason}", userId);

        _ledgerEntries.Add(entry);
        RaiseDomainEvent(new StockAdjustedEvent(Id, VariantId, StockQuantity, reversalDelta, reason));

        return Result.Success();
    }

    public Result ReturnStock(
        StockQuantity quantity,
        string reason,
        UserId? userId = null)
    {
        if (quantity <= 0) return Result.Failure(new Error("Inventory.InvalidQuantity", "مقدار باید بزرگتر از صفر باشد."));
        if (IsUnlimited) return Result.Success();

        var currentStock = ValueObjects.StockQuantity.Create(StockQuantity);
        StockQuantity = currentStock.Add(quantity);

        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var entry = StockLedgerEntry.StockIn(
            VariantId, quantity, StockQuantity, 0, null, reason, userId: userId);

        _ledgerEntries.Add(entry);
        RaiseDomainEvent(new StockRestoredEvent(Id, VariantId, StockQuantity, quantity, reason));

        return Result.Success();
    }

    public Result AdjustStock(int quantityChange, UserId userId, string reason)
    {
        if (IsUnlimited) return Result.Failure(new Error("Inventory.NotApplicable", "واریانت نامحدود قابل تنظیم دستی نیست."));

        var currentStock = ValueObjects.StockQuantity.Create(StockQuantity);

        if (quantityChange < 0)
        {
            var subtractResult = currentStock.TrySubtract(Math.Abs(quantityChange));
            if (subtractResult.IsFailure) return Result.Failure(subtractResult.Error);
            StockQuantity = subtractResult.Value;
        }
        else
        {
            StockQuantity = currentStock.Add(quantityChange);
        }

        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var entry = StockLedgerEntry.Adjustment(VariantId, quantityChange, StockQuantity, reason, userId);
        _ledgerEntries.Add(entry);

        RaiseDomainEvent(new StockAdjustedEvent(Id, VariantId, StockQuantity, quantityChange, reason));

        return Result.Success();
    }

    public Result RecordDamage(
        int quantity,
        UserId userId,
        string reason)
    {
        if (quantity <= 0) return Result.Failure(new Error("Inventory.InvalidQuantity", "مقدار باید بزرگتر از صفر باشد."));
        if (IsUnlimited) return Result.Failure(new Error("Inventory.NotApplicable", "واریانت نامحدود قابل ثبت خسارت نیست."));

        var currentStock = ValueObjects.StockQuantity.Create(StockQuantity);
        var subtractResult = currentStock.TrySubtract(quantity);
        if (subtractResult.IsFailure) return Result.Failure(subtractResult.Error);

        StockQuantity = subtractResult.Value;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var entry = StockLedgerEntry.Adjustment(
            VariantId, -quantity, StockQuantity, $"ضایعات: {reason}", userId);

        _ledgerEntries.Add(entry);

        return Result.Success();
    }

    public Result Reconcile(StockQuantity calculatedStockFromLedger, UserId userId)
    {
        if (IsUnlimited) return Result.Success();

        var difference = calculatedStockFromLedger - StockQuantity;
        if (difference == 0) return Result.Success();

        StockQuantity = calculatedStockFromLedger;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var entry = StockLedgerEntry.Adjustment(
            VariantId, difference, StockQuantity,
            $"انبارگردانی: اختلاف {difference} واحد", userId);

        _ledgerEntries.Add(entry);

        return Result.Success();
    }

    public Result SetLowStockThreshold(int threshold)
    {
        if (threshold < 0) return Result.Failure(new Error("Inventory.InvalidThreshold", "آستانه کم‌موجودی نمی‌تواند منفی باشد."));

        LowStockThreshold = threshold;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        return Result.Success();
    }

    public bool CanFulfill(int quantity) =>
        IsUnlimited || AvailableQuantity >= quantity;
}