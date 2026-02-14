namespace Domain.Inventory.Services;

/// <summary>
/// Domain Service برای عملیات‌های موجودی که بین چند Aggregate هستند
/// Stateless - بدون وابستگی به Infrastructure
/// </summary>
public class InventoryDomainService
{
    /// <summary>
    /// رزرو موجودی برای سفارش
    /// </summary>
    public InventoryReservationResult Reserve(
        ProductVariant variant,
        int quantity,
        int orderItemId,
        int? userId = null,
        string? referenceNumber = null)
    {
        Guard.Against.Null(variant, nameof(variant));
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        if (!variant.IsActive || variant.IsDeleted)
        {
            return InventoryReservationResult.Failed(
                variant.Id,
                $"واریانت {variant.Sku ?? variant.Id.ToString()} غیرفعال یا حذف شده است.");
        }

        if (variant.IsUnlimited)
        {
            return InventoryReservationResult.SuccessUnlimited(variant.Id, quantity);
        }

        if (variant.AvailableStock < quantity)
        {
            return InventoryReservationResult.InsufficientStock(
                variant.Id,
                variant.AvailableStock,
                quantity);
        }

        var transaction = InventoryTransaction.CreateReservation(
            variant.Id,
            quantity,
            variant.StockQuantity,
            orderItemId,
            userId,
            referenceNumber);

        variant.Reserve(quantity);

        return InventoryReservationResult.Success(variant.Id, quantity, transaction);
    }

    /// <summary>
    /// تأیید رزرو پس از پرداخت موفق - کسر نهایی از موجودی
    /// </summary>
    public InventorySaleResult ConfirmReservation(
        ProductVariant variant,
        int quantity,
        int orderItemId,
        int? userId = null,
        string? referenceNumber = null)
    {
        Guard.Against.Null(variant, nameof(variant));
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        if (variant.IsUnlimited)
        {
            return InventorySaleResult.SuccessUnlimited(variant.Id, quantity);
        }

        variant.ConfirmReservation(quantity);

        var transaction = InventoryTransaction.CreateSale(
            variant.Id,
            quantity,
            variant.StockQuantity + quantity,
            orderItemId,
            userId,
            referenceNumber);

        return InventorySaleResult.Success(variant.Id, quantity, transaction);
    }

    /// <summary>
    /// برگشت رزرو در صورت عدم پرداخت یا لغو سفارش
    /// </summary>
    public InventoryReleaseResult RollbackReservation(
        ProductVariant variant,
        int quantity,
        int? userId = null,
        string? reason = null)
    {
        Guard.Against.Null(variant, nameof(variant));
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));

        if (variant.IsUnlimited)
        {
            return InventoryReleaseResult.SuccessUnlimited(variant.Id);
        }

        var actualRelease = Math.Min(quantity, variant.ReservedQuantity);
        if (actualRelease == 0)
        {
            return InventoryReleaseResult.NothingToRelease(variant.Id);
        }

        variant.Release(actualRelease);

        var transaction = InventoryTransaction.Create(
            variant.Id,
            TransactionType.ReservationRollback,
            actualRelease,
            variant.StockQuantity - actualRelease,
            userId,
            reason ?? "آزادسازی موجودی رزرو شده");

        return InventoryReleaseResult.Success(variant.Id, actualRelease, transaction);
    }

    /// <summary>
    /// تنظیم دستی موجودی توسط مدیر
    /// </summary>
    public StockAdjustmentResult AdjustStock(
        ProductVariant variant,
        int quantityChange,
        int userId,
        string notes)
    {
        Guard.Against.Null(variant, nameof(variant));
        Guard.Against.NegativeOrZero(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(notes, nameof(notes));

        if (variant.IsUnlimited)
        {
            return StockAdjustmentResult.NotApplicable(variant.Id, "واریانت نامحدود قابل تنظیم دستی نیست.");
        }

        var newStock = variant.StockQuantity + quantityChange;
        if (newStock < 0)
        {
            return StockAdjustmentResult.Failed(
                variant.Id,
                $"تنظیم موجودی منجر به مقدار منفی ({newStock}) می‌شود. موجودی فعلی: {variant.StockQuantity}");
        }

        var transaction = InventoryTransaction.CreateAdjustment(
            variant.Id,
            quantityChange,
            variant.StockQuantity,
            userId,
            notes);

        variant.AdjustStock(quantityChange);

        return StockAdjustmentResult.Success(variant.Id, variant.StockQuantity, transaction);
    }

    /// <summary>
    /// ثبت خسارت و کاهش موجودی
    /// </summary>
    public StockAdjustmentResult RecordDamage(
        ProductVariant variant,
        int quantity,
        int userId,
        string notes)
    {
        Guard.Against.Null(variant, nameof(variant));
        Guard.Against.NegativeOrZero(quantity, nameof(quantity));
        Guard.Against.NegativeOrZero(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(notes, nameof(notes));

        if (variant.IsUnlimited)
        {
            return StockAdjustmentResult.NotApplicable(variant.Id, "واریانت نامحدود قابل ثبت خسارت نیست.");
        }

        if (variant.StockQuantity < quantity)
        {
            return StockAdjustmentResult.Failed(
                variant.Id,
                $"موجودی کافی برای ثبت خسارت نیست. موجودی فعلی: {variant.StockQuantity}، خسارت: {quantity}");
        }

        var transaction = InventoryTransaction.CreateDamage(
            variant.Id,
            quantity,
            variant.StockQuantity,
            userId,
            notes);

        variant.AdjustStock(-quantity);

        return StockAdjustmentResult.Success(variant.Id, variant.StockQuantity, transaction);
    }

    /// <summary>
    /// اعتبارسنجی امکان کسر موجودی
    /// </summary>
    public StockValidationResult ValidateStockDeduction(
        ProductVariant variant,
        int quantity)
    {
        Guard.Against.Null(variant, nameof(variant));

        if (variant.IsUnlimited)
            return StockValidationResult.Valid();

        if (!variant.IsActive || variant.IsDeleted)
            return StockValidationResult.Invalid("محصول غیرفعال است.");

        if (variant.AvailableStock < quantity)
        {
            return StockValidationResult.InsufficientStock(
                variant.AvailableStock,
                quantity);
        }

        return StockValidationResult.Valid();
    }

    /// <summary>
    /// محاسبه وضعیت موجودی چند واریانت (برای سفارش‌های دسته‌ای)
    /// </summary>
    public BatchStockStatus CalculateBatchStockStatus(
        IEnumerable<(ProductVariant Variant, int RequestedQuantity)> items)
    {
        var itemsList = items.ToList();
        var results = new List<VariantStockStatus>();
        var allAvailable = true;

        foreach (var (variant, quantity) in itemsList)
        {
            var validation = ValidateStockDeduction(variant, quantity);

            var status = new VariantStockStatus(
                variant.Id,
                variant.AvailableStock,
                quantity,
                variant.IsUnlimited,
                validation.IsValid);

            results.Add(status);

            if (!validation.IsValid)
                allAvailable = false;
        }

        return new BatchStockStatus(results, allAvailable);
    }

    /// <summary>
    /// بررسی نیاز به هشدار کم‌موجودی
    /// </summary>
    public LowStockCheckResult CheckLowStock(
        ProductVariant variant,
        int? customThreshold = null)
    {
        Guard.Against.Null(variant, nameof(variant));

        if (variant.IsUnlimited)
            return LowStockCheckResult.NotApplicable();

        var threshold = customThreshold ?? variant.LowStockThreshold;

        if (variant.AvailableStock <= 0)
            return LowStockCheckResult.OutOfStock(variant.Id, variant.AvailableStock);

        if (variant.AvailableStock <= threshold)
            return LowStockCheckResult.LowStock(variant.Id, variant.AvailableStock, threshold);

        return LowStockCheckResult.Healthy(variant.AvailableStock);
    }

    /// <summary>
    /// انبارگردانی - مقایسه موجودی فعلی با موجودی محاسبه‌شده از تراکنش‌ها
    /// </summary>
    public ReconcileResult Reconcile(
        ProductVariant variant,
        int calculatedStockFromTransactions,
        int userId)
    {
        Guard.Against.Null(variant, nameof(variant));
        Guard.Against.NegativeOrZero(userId, nameof(userId));

        if (variant.IsUnlimited)
            return ReconcileResult.NotApplicable(variant.Id);

        var difference = calculatedStockFromTransactions - variant.StockQuantity;

        if (difference == 0)
            return ReconcileResult.NoDiscrepancy(variant.Id, variant.StockQuantity);

        var transaction = InventoryTransaction.CreateAdjustment(
            variant.Id,
            difference,
            variant.StockQuantity,
            userId,
            $"انبارگردانی: اختلاف {difference} واحد (محاسبه‌شده: {calculatedStockFromTransactions}، فعلی: {variant.StockQuantity})");

        variant.AdjustStock(difference);

        return ReconcileResult.Corrected(variant.Id, variant.StockQuantity, difference, transaction);
    }
}

#region Result Types

public sealed class InventoryReservationResult
{
    public bool IsSuccess { get; private set; }
    public int VariantId { get; private set; }
    public int ReservedQuantity { get; private set; }
    public string? Error { get; private set; }
    public bool IsUnlimited { get; private set; }
    public int? AvailableStock { get; private set; }
    public int? RequestedQuantity { get; private set; }
    public InventoryTransaction? Transaction { get; private set; }

    private InventoryReservationResult() { }

    public static InventoryReservationResult Success(int variantId, int quantity, InventoryTransaction transaction)
        => new() { IsSuccess = true, VariantId = variantId, ReservedQuantity = quantity, Transaction = transaction };

    public static InventoryReservationResult SuccessUnlimited(int variantId, int quantity)
        => new() { IsSuccess = true, VariantId = variantId, ReservedQuantity = quantity, IsUnlimited = true };

    public static InventoryReservationResult InsufficientStock(int variantId, int available, int requested)
        => new()
        {
            IsSuccess = false,
            VariantId = variantId,
            AvailableStock = available,
            RequestedQuantity = requested,
            Error = $"موجودی کافی نیست. موجودی: {available}، درخواستی: {requested}"
        };

    public static InventoryReservationResult Failed(int variantId, string error)
        => new() { IsSuccess = false, VariantId = variantId, Error = error };

    public int GetShortage() => RequestedQuantity.HasValue && AvailableStock.HasValue
        ? RequestedQuantity.Value - AvailableStock.Value
        : 0;
}

public sealed class InventoryReleaseResult
{
    public bool IsSuccess { get; private set; }
    public int VariantId { get; private set; }
    public int ReleasedQuantity { get; private set; }
    public InventoryTransaction? Transaction { get; private set; }
    public string? Message { get; private set; }

    private InventoryReleaseResult() { }

    public static InventoryReleaseResult Success(int variantId, int quantity, InventoryTransaction transaction)
        => new() { IsSuccess = true, VariantId = variantId, ReleasedQuantity = quantity, Transaction = transaction };

    public static InventoryReleaseResult SuccessUnlimited(int variantId)
        => new() { IsSuccess = true, VariantId = variantId, Message = "واریانت نامحدود - نیازی به آزادسازی نیست" };

    public static InventoryReleaseResult NothingToRelease(int variantId)
        => new() { IsSuccess = true, VariantId = variantId, ReleasedQuantity = 0, Message = "موجودی رزرو شده‌ای برای آزادسازی وجود ندارد" };
}

public sealed class InventorySaleResult
{
    public bool IsSuccess { get; private set; }
    public int VariantId { get; private set; }
    public int SoldQuantity { get; private set; }
    public InventoryTransaction? Transaction { get; private set; }
    public string? Error { get; private set; }

    private InventorySaleResult() { }

    public static InventorySaleResult Success(int variantId, int quantity, InventoryTransaction transaction)
        => new() { IsSuccess = true, VariantId = variantId, SoldQuantity = quantity, Transaction = transaction };

    public static InventorySaleResult SuccessUnlimited(int variantId, int quantity)
        => new() { IsSuccess = true, VariantId = variantId, SoldQuantity = quantity };

    public static InventorySaleResult Failed(int variantId, string error)
        => new() { IsSuccess = false, VariantId = variantId, Error = error };
}

public sealed class StockAdjustmentResult
{
    public bool IsSuccess { get; private set; }
    public int VariantId { get; private set; }
    public int NewStock { get; private set; }
    public string? Error { get; private set; }
    public InventoryTransaction? Transaction { get; private set; }

    private StockAdjustmentResult() { }

    public static StockAdjustmentResult Success(int variantId, int newStock, InventoryTransaction transaction)
        => new() { IsSuccess = true, VariantId = variantId, NewStock = newStock, Transaction = transaction };

    public static StockAdjustmentResult Failed(int variantId, string error)
        => new() { IsSuccess = false, VariantId = variantId, Error = error };

    public static StockAdjustmentResult NotApplicable(int variantId, string reason)
        => new() { IsSuccess = false, VariantId = variantId, Error = reason };
}

public sealed class StockValidationResult
{
    public bool IsValid { get; private set; }
    public string? Error { get; private set; }
    public int? AvailableStock { get; private set; }
    public int? RequestedQuantity { get; private set; }

    private StockValidationResult() { }

    public static StockValidationResult Valid()
        => new() { IsValid = true };

    public static StockValidationResult Invalid(string error)
        => new() { IsValid = false, Error = error };

    public static StockValidationResult InsufficientStock(int available, int requested)
        => new()
        {
            IsValid = false,
            AvailableStock = available,
            RequestedQuantity = requested,
            Error = $"موجودی کافی نیست. موجودی: {available}، درخواستی: {requested}"
        };
}

public sealed record VariantStockStatus(
    int VariantId,
    int AvailableStock,
    int RequestedQuantity,
    bool IsUnlimited,
    bool CanFulfill)
{
    public int Shortage => CanFulfill ? 0 : RequestedQuantity - AvailableStock;
}

public sealed class BatchStockStatus
{
    public IReadOnlyList<VariantStockStatus> Items { get; }
    public bool AllAvailable { get; }
    public int TotalShortage { get; }

    public BatchStockStatus(IReadOnlyList<VariantStockStatus> items, bool allAvailable)
    {
        Items = items;
        AllAvailable = allAvailable;
        TotalShortage = items.Sum(i => i.Shortage);
    }

    public IEnumerable<VariantStockStatus> GetUnavailableItems()
        => Items.Where(i => !i.CanFulfill);
}

public sealed class LowStockCheckResult
{
    public bool IsLowStock { get; private set; }
    public bool IsOutOfStock { get; private set; }
    public int VariantId { get; private set; }
    public int CurrentStock { get; private set; }
    public int? Threshold { get; private set; }
    public bool IsApplicable { get; private set; } = true;

    private LowStockCheckResult() { }

    public static LowStockCheckResult Healthy(int currentStock)
        => new() { IsLowStock = false, IsOutOfStock = false, CurrentStock = currentStock };

    public static LowStockCheckResult LowStock(int variantId, int currentStock, int threshold)
        => new() { IsLowStock = true, IsOutOfStock = false, VariantId = variantId, CurrentStock = currentStock, Threshold = threshold };

    public static LowStockCheckResult OutOfStock(int variantId, int currentStock)
        => new() { IsLowStock = true, IsOutOfStock = true, VariantId = variantId, CurrentStock = currentStock };

    public static LowStockCheckResult NotApplicable()
        => new() { IsApplicable = false };
}

public sealed class ReconcileResult
{
    public bool IsSuccess { get; private set; }
    public int VariantId { get; private set; }
    public int FinalStock { get; private set; }
    public int Difference { get; private set; }
    public bool HasDiscrepancy { get; private set; }
    public string? Message { get; private set; }
    public InventoryTransaction? Transaction { get; private set; }

    private ReconcileResult() { }

    public static ReconcileResult Corrected(int variantId, int finalStock, int difference, InventoryTransaction transaction)
        => new()
        {
            IsSuccess = true,
            VariantId = variantId,
            FinalStock = finalStock,
            Difference = difference,
            HasDiscrepancy = true,
            Transaction = transaction,
            Message = $"اختلاف {difference} واحد اصلاح شد."
        };

    public static ReconcileResult NoDiscrepancy(int variantId, int currentStock)
        => new()
        {
            IsSuccess = true,
            VariantId = variantId,
            FinalStock = currentStock,
            Difference = 0,
            HasDiscrepancy = false,
            Message = "اختلافی یافت نشد."
        };

    public static ReconcileResult NotApplicable(int variantId)
        => new()
        {
            IsSuccess = false,
            VariantId = variantId,
            Message = "واریانت نامحدود قابل انبارگردانی نیست."
        };
}

#endregion