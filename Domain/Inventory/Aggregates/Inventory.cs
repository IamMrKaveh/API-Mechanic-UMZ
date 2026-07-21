using Domain.Inventory.Entities;
using Domain.Inventory.Events;
using Domain.Inventory.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Domain.Inventory.Aggregates;

public sealed class Inventory : AggregateRoot<InventoryId>, ISoftDeletable
{
    private Inventory()
    { }

    public StockQuantity StockQuantity { get; private set; }
    public bool IsUnlimited { get; private set; }
    public StockQuantity ReservedQuantity { get; private set; }
    public int LowStockThreshold { get; private set; } = 5;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public VariantId VariantId { get; private set; } = default!;
    public Variant.Aggregates.ProductVariant Variant { get; private set; } = default!;
    private readonly List<StockLedgerEntry> _ledgerEntries = [];
    public IReadOnlyCollection<StockLedgerEntry> LedgerEntries => _ledgerEntries.AsReadOnly();

    public int AvailableQuantity => IsUnlimited ? int.MaxValue : StockQuantity - ReservedQuantity;

    public bool IsInStock => IsUnlimited || AvailableQuantity > 0;
    public bool IsOutOfStock => !IsUnlimited && AvailableQuantity <= 0;

    public bool IsLowStock =>
        !IsUnlimited && AvailableQuantity > 0 && AvailableQuantity <= LowStockThreshold;

    public static Inventory Create(
        VariantId variantId,
        int initialStock = 0,
        bool isUnlimited = false,
        int lowStockThreshold = 5,
        UserId? createdBy = null)
    {
        Guard.Against.Null(variantId, nameof(variantId));
        if (initialStock < 0)
            throw new DomainException("موجودی اولیه نمی‌تواند منفی باشد.");
        if (lowStockThreshold < 0)
            throw new DomainException("آستانه کمبود موجودی نمی‌تواند منفی باشد.");

        var inventory = new Inventory
        {
            Id = InventoryId.NewId(),
            VariantId = variantId,
            StockQuantity = StockQuantity.Create(initialStock),
            ReservedQuantity = StockQuantity.Create(0),
            IsUnlimited = isUnlimited,
            LowStockThreshold = lowStockThreshold,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        if (initialStock > 0 && !isUnlimited)
        {
            var entry = StockLedgerEntry.StockIn(
                variantId,
                initialStock,
                StockQuantity.Create(initialStock),
                0,
                null,
                "ایجاد موجودی اولیه برای واریانت",
                userId: createdBy);
            inventory._ledgerEntries.Add(entry);
        }

        inventory.RaiseDomainEvent(new InventoryCreatedEvent(inventory.Id, variantId, initialStock, isUnlimited));
        return inventory;
    }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public Guid? DeletedBy { get; private set; }

    public ServiceResult IncreaseStock(
        int quantity,
        string reason,
        UserId? userId = null,
        string? referenceNumber = null)
    {
        if (quantity <= 0) return ServiceResult.Failure(new Error("Inventory.InvalidQuantity", "مقدار باید بزرگتر از صفر باشد."));

        var currentStock = StockQuantity.Create(StockQuantity);
        StockQuantity = currentStock.Add(quantity);

        IsUnlimited = false;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var entry = StockLedgerEntry.StockIn(
            VariantId, quantity, StockQuantity, 0, referenceNumber, reason, userId: userId);

        _ledgerEntries.Add(entry);
        RaiseDomainEvent(new StockIncreasedEvent(Id, VariantId, quantity, StockQuantity, reason));

        return ServiceResult.Success();
    }

    public ServiceResult DecreaseStock(
        int quantity,
        string reason,
        UserId? userId = null,
        string? referenceNumber = null)
    {
        if (quantity <= 0) return ServiceResult.Failure(new Error("Inventory.InvalidQuantity", "مقدار باید بزرگتر از صفر باشد."));

        if (!IsUnlimited)
        {
            var currentStock = StockQuantity.Create(StockQuantity);
            var subtractResult = currentStock.TrySubtract(quantity);
            if (subtractResult.IsFailure) return ServiceResult.Failure(subtractResult.Error);

            StockQuantity = subtractResult.Value;
        }

        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var entry = StockLedgerEntry.Adjustment(
            VariantId, -quantity, IsUnlimited ? 0 : StockQuantity, reason, userId);

        _ledgerEntries.Add(entry);
        RaiseDomainEvent(
            new StockDecreasedEvent(Id, VariantId, quantity, IsUnlimited ? -1 : StockQuantity, reason));

        return ServiceResult.Success();
    }

    public void SetUnlimited()
    {
        IsUnlimited = true;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
        RaiseDomainEvent(new StockSetUnlimitedEvent(Id, VariantId));
    }

    public void SetLowStockThreshold(int threshold)
    {
        if (threshold < 0)
            throw new DomainException("آستانه کمبود موجودی نمی‌تواند منفی باشد.");
        LowStockThreshold = threshold;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();
    }

    public ServiceResult ReserveStock(
        StockQuantity quantity,
        string referenceNumber,
        OrderItemId? orderItemId = null,
        UserId? userId = null,
        string? correlationId = null)
    {
        if (quantity <= 0) return ServiceResult.Failure(new Error("Inventory.InvalidQuantity", "مقدار باید بزرگتر از صفر باشد."));

        if (!IsUnlimited)
        {
            var available = StockQuantity.Create(AvailableQuantity);
            var reserveResult = available.TrySubtract(quantity);

            if (reserveResult.IsFailure) return ServiceResult.Failure(reserveResult.Error);

            ReservedQuantity = ReservedQuantity.Add(quantity);
        }

        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var entry = StockLedgerEntry.Reserve(
            VariantId, quantity, IsUnlimited ? 0 : AvailableQuantity,
            referenceNumber, correlationId, userId: userId, orderItemId: orderItemId);

        _ledgerEntries.Add(entry);
        RaiseDomainEvent(new StockReservedEvent(Id, VariantId, quantity, ReservedQuantity));

        return ServiceResult.Success();
    }

    public ServiceResult ReleaseReservation(StockQuantity quantity, string referenceNumber, string? reason = null)
    {
        if (quantity <= 0)
            return ServiceResult.Failure(new Error("Inventory.InvalidQuantity", "مقدار باید بزرگتر از صفر باشد."));
        if (IsUnlimited)
            return ServiceResult.Success();

        var actualRelease = Math.Min(quantity, ReservedQuantity);
        if (actualRelease == 0)
            return ServiceResult.Success();

        ReservedQuantity = ReservedQuantity.Subtract(actualRelease);
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var entry = StockLedgerEntry.ReleaseReservation(
            VariantId, actualRelease, AvailableQuantity, referenceNumber, reason);

        _ledgerEntries.Add(entry);
        RaiseDomainEvent(
            new StockReservationReleasedEvent(Id, VariantId, actualRelease, ReservedQuantity));

        return ServiceResult.Success();
    }

    public ServiceResult ConfirmReservation(
        StockQuantity quantity,
        string referenceNumber,
        OrderItemId? orderItemId = null)
    {
        if (quantity <= 0) return ServiceResult.Failure(new Error("Inventory.InvalidQuantity", "مقدار باید بزرگتر از صفر باشد."));
        if (IsUnlimited) return ServiceResult.Success();

        if (ReservedQuantity < quantity)
            return ServiceResult.Failure(new Error("Inventory.InsufficientReservation", $"موجودی رزرو شده کافی نیست. رزرو شده: {ReservedQuantity}، درخواستی: {quantity}"));

        ReservedQuantity = ReservedQuantity.Subtract(quantity.Value);
        StockQuantity = StockQuantity.Subtract(quantity.Value);

        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var entry = StockLedgerEntry.CommitReservation(
            VariantId, quantity, StockQuantity, referenceNumber, orderItemId);

        _ledgerEntries.Add(entry);
        RaiseDomainEvent(new StockCommittedEvent(Id, VariantId, OrderItemId.NewId(), quantity));

        return ServiceResult.Success();
    }

    public ServiceResult ReverseStockChange(
        string idempotencyKey,
        string reason,
        UserId userId)
    {
        if (IsUnlimited) return ServiceResult.Failure(new Error("Inventory.NotApplicable", "امکان برگشت تراکنش برای واریانت نامحدود وجود ندارد."));

        var originalEntry = _ledgerEntries.FirstOrDefault(e => e.IdempotencyKey == idempotencyKey);
        if (originalEntry == null) return ServiceResult.Failure(new Error("Inventory.NotFound", "تراکنش مورد نظر یافت نشد."));

        var reversalDelta = -originalEntry.QuantityDelta;
        var currentStock = StockQuantity.Create(StockQuantity);

        if (reversalDelta < 0)
        {
            var subtractResult = currentStock.TrySubtract(Math.Abs(reversalDelta));
            if (subtractResult.IsFailure) return ServiceResult.Failure(subtractResult.Error);
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

        return ServiceResult.Success();
    }

    public ServiceResult ReturnStock(
        StockQuantity quantity,
        string reason,
        UserId? userId = null)
    {
        if (quantity <= 0) return ServiceResult.Failure(new Error("Inventory.InvalidQuantity", "مقدار باید بزرگتر از صفر باشد."));
        if (IsUnlimited) return ServiceResult.Success();

        var currentStock = StockQuantity.Create(StockQuantity);
        StockQuantity = currentStock.Add(quantity);

        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var entry = StockLedgerEntry.StockIn(
            VariantId, quantity, StockQuantity, 0, null, reason, userId: userId);

        _ledgerEntries.Add(entry);
        RaiseDomainEvent(new StockRestoredEvent(Id, VariantId, StockQuantity, quantity, reason));

        return ServiceResult.Success();
    }

    public ServiceResult AdjustStock(int quantityChange, UserId userId, string reason)
    {
        if (IsUnlimited) return ServiceResult.Failure(new Error("Inventory.NotApplicable", "واریانت نامحدود قابل تنظیم دستی نیست."));

        var currentStock = StockQuantity.Create(StockQuantity);

        if (quantityChange < 0)
        {
            var subtractResult = currentStock.TrySubtract(Math.Abs(quantityChange));
            if (subtractResult.IsFailure) return ServiceResult.Failure(subtractResult.Error);
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

        return ServiceResult.Success();
    }

    public ServiceResult AdjustStockTo(
    int targetQuantity,
    string reason,
    UserId? userId = null,
    string? referenceNumber = null)
    {
        if (targetQuantity < 0)
            return ServiceResult.Failure(new Error(
                "Inventory.InvalidQuantity",
                "موجودی هدف نمی‌تواند منفی باشد."));

        if (IsUnlimited)
            return ServiceResult.Success();

        var diff = targetQuantity - StockQuantity.Value;

        if (diff == 0)
            return ServiceResult.Success();

        return diff > 0
            ? IncreaseStock(diff, reason, userId, referenceNumber)
            : DecreaseStock(-diff, reason, userId, referenceNumber);
    }

    public ServiceResult RecordDamage(
        int quantity,
        UserId userId,
        string reason)
    {
        if (quantity <= 0) return ServiceResult.Failure(new Error("Inventory.InvalidQuantity", "مقدار باید بزرگتر از صفر باشد."));
        if (IsUnlimited) return ServiceResult.Failure(new Error("Inventory.NotApplicable", "واریانت نامحدود قابل ثبت خسارت نیست."));

        var currentStock = StockQuantity.Create(StockQuantity);
        var subtractResult = currentStock.TrySubtract(quantity);
        if (subtractResult.IsFailure) return ServiceResult.Failure(subtractResult.Error);

        StockQuantity = subtractResult.Value;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var entry = StockLedgerEntry.Adjustment(
            VariantId, -quantity, StockQuantity, $"ضایعات: {reason}", userId);

        _ledgerEntries.Add(entry);

        return ServiceResult.Success();
    }

    public ServiceResult Reconcile(StockQuantity calculatedStockFromLedger, UserId userId)
    {
        if (IsUnlimited) return ServiceResult.Success();

        var difference = calculatedStockFromLedger - StockQuantity;
        if (difference == 0) return ServiceResult.Success();

        StockQuantity = calculatedStockFromLedger;
        UpdatedAt = DateTime.UtcNow;
        IncrementVersion();

        var entry = StockLedgerEntry.Adjustment(
            VariantId, difference, StockQuantity,
            $"انبارگردانی: اختلاف {difference} واحد", userId);

        _ledgerEntries.Add(entry);

        return ServiceResult.Success();
    }

    public bool CanFulfill(int quantity) =>
        IsUnlimited || AvailableQuantity >= quantity;
}
