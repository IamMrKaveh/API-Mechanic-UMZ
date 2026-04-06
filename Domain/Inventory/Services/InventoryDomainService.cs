using Domain.Inventory.Services.Results;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Inventory.Services;

public sealed class InventoryDomainService
{
    public ReservationResult Reserve(
        Aggregates.Inventory inventory,
        int quantity,
        string referenceNumber,
        OrderItemId? orderItemId = null,
        UserId? userId = null,
        string? correlationId = null)
    {
        Guard.Against.Null(inventory, nameof(inventory));
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        if (!inventory.CanFulfill(quantity))
        {
            return ReservationResult.InsufficientStock(
                inventory.VariantId,
                inventory.AvailableQuantity,
                quantity);
        }

        inventory.ReserveStock(quantity, referenceNumber, orderItemId, userId, correlationId);

        return ReservationResult.Success(inventory.VariantId, quantity, inventory.IsUnlimited);
    }

    public ConfirmationResult ConfirmReservation(
        Aggregates.Inventory inventory,
        int quantity,
        string referenceNumber,
        OrderItemId? orderItemId = null)
    {
        Guard.Against.Null(inventory, nameof(inventory));
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        if (inventory.IsUnlimited)
            return ConfirmationResult.Success(inventory.VariantId, quantity);

        if (inventory.ReservedQuantity < quantity)
        {
            return ConfirmationResult.Failed(
                inventory.VariantId,
                $"موجودی رزرو شده کافی نیست. رزرو شده: {inventory.ReservedQuantity}، درخواستی: {quantity}");
        }

        inventory.ConfirmReservation(quantity, referenceNumber, orderItemId);

        return ConfirmationResult.Success(inventory.VariantId, quantity);
    }

    public ReleaseResult RollbackReservation(
        Aggregates.Inventory inventory,
        int quantity,
        string referenceNumber,
        string? reason = null)
    {
        Guard.Against.Null(inventory, nameof(inventory));
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        if (inventory.IsUnlimited)
            return ReleaseResult.NotApplicable(inventory.VariantId, "واریانت نامحدود - نیازی به آزادسازی نیست");

        var actualRelease = Math.Min(quantity, inventory.ReservedQuantity);
        if (actualRelease == 0)
            return ReleaseResult.NothingToRelease(inventory.VariantId);

        inventory.ReleaseReservation(actualRelease, referenceNumber, reason);

        return ReleaseResult.Success(inventory.VariantId, actualRelease);
    }

    public AdjustmentResult ReturnStock(
        Aggregates.Inventory inventory,
        int quantity,
        string reason,
        OrderItemId? orderItemId = null,
        UserId? userId = null)
    {
        Guard.Against.Null(inventory, nameof(inventory));
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));

        if (inventory.IsUnlimited)
            return AdjustmentResult.NotApplicable(inventory.VariantId, "واریانت نامحدود - مرجوعی تأثیری بر موجودی ندارد.");

        inventory.ReturnStock(quantity, reason, orderItemId, userId);

        return AdjustmentResult.Success(inventory.VariantId, inventory.StockQuantity);
    }

    public AdjustmentResult AdjustStock(
        Aggregates.Inventory inventory,
        int quantityChange,
        UserId userId,
        string reason)
    {
        Guard.Against.Null(inventory, nameof(inventory));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));

        if (inventory.IsUnlimited)
            return AdjustmentResult.NotApplicable(inventory.VariantId, "واریانت نامحدود قابل تنظیم دستی نیست.");

        var newStock = inventory.StockQuantity + quantityChange;
        if (newStock < 0)
        {
            return AdjustmentResult.Failed(
                inventory.VariantId,
                $"تنظیم موجودی منجر به مقدار منفی ({newStock}) می‌شود. موجودی فعلی: {inventory.StockQuantity}");
        }

        inventory.AdjustStock(quantityChange, userId, reason);

        return AdjustmentResult.Success(inventory.VariantId, inventory.StockQuantity);
    }

    public AdjustmentResult RecordDamage(
        Aggregates.Inventory inventory,
        int quantity,
        UserId userId,
        string reason)
    {
        Guard.Against.Null(inventory, nameof(inventory));
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));

        if (inventory.IsUnlimited)
            return AdjustmentResult.NotApplicable(inventory.VariantId, "واریانت نامحدود قابل ثبت خسارت نیست.");

        if (inventory.StockQuantity < quantity)
        {
            return AdjustmentResult.Failed(
                inventory.VariantId,
                $"موجودی کافی برای ثبت خسارت نیست. موجودی فعلی: {inventory.StockQuantity}، خسارت: {quantity}");
        }

        inventory.RecordDamage(quantity, userId, reason);

        return AdjustmentResult.Success(inventory.VariantId, inventory.StockQuantity);
    }

    public StockValidationResult ValidateStockDeduction(Aggregates.Inventory inventory, int quantity)
    {
        Guard.Against.Null(inventory, nameof(inventory));

        if (inventory.IsUnlimited)
            return StockValidationResult.Valid();

        if (inventory.AvailableQuantity < quantity)
            return StockValidationResult.InsufficientStock(inventory.AvailableQuantity, quantity);

        return StockValidationResult.Valid();
    }

    public BatchStockStatus CalculateBatchStockStatus(
        IEnumerable<(Aggregates.Inventory Inventory, int RequestedQuantity)> items)
    {
        var itemsList = items.ToList();
        var results = new List<VariantStockStatus>();
        var allAvailable = true;

        foreach (var (inventory, quantity) in itemsList)
        {
            var validation = ValidateStockDeduction(inventory, quantity);
            var status = new VariantStockStatus(
                inventory.VariantId,
                inventory.AvailableQuantity,
                quantity,
                inventory.IsUnlimited,
                validation.IsValid);
            results.Add(status);
            if (!validation.IsValid)
                allAvailable = false;
        }

        return new BatchStockStatus(results, allAvailable);
    }

    public LowStockCheckResult CheckLowStock(Aggregates.Inventory inventory)
    {
        Guard.Against.Null(inventory, nameof(inventory));

        if (inventory.IsUnlimited)
            return LowStockCheckResult.NotApplicable();

        if (inventory.IsOutOfStock)
            return LowStockCheckResult.OutOfStock(inventory.VariantId, inventory.AvailableQuantity);

        if (inventory.IsLowStock)
            return LowStockCheckResult.LowStock(inventory.VariantId, inventory.AvailableQuantity, inventory.LowStockThreshold);

        return LowStockCheckResult.Healthy(inventory.AvailableQuantity);
    }

    public ReconcileResult Reconcile(
        Aggregates.Inventory inventory,
        int calculatedStockFromTransactions,
        UserId userId)
    {
        Guard.Against.Null(inventory, nameof(inventory));

        if (inventory.IsUnlimited)
            return ReconcileResult.NotApplicable(inventory.VariantId);

        var difference = calculatedStockFromTransactions - inventory.StockQuantity;

        if (difference == 0)
            return ReconcileResult.NoDiscrepancy(inventory.VariantId, inventory.StockQuantity);

        inventory.Reconcile(calculatedStockFromTransactions, userId);

        return ReconcileResult.Corrected(inventory.VariantId, inventory.StockQuantity, difference);
    }
}