namespace Domain.Inventory.Services;

/// <summary>
/// Domain Service برای هماهنگی رزرو موجودی در سناریوی Inventory + Order
/// Stateless - فقط Domain Type می‌گیرد و Domain Result برمی‌گرداند
/// </summary>
public sealed class InventoryReservationService
{
    /// <summary>
    /// اعتبارسنجی امکان رزرو دسته‌ای برای checkout - بدون اعمال تغییر
    /// </summary>
    public BatchReservationValidation ValidateBatchAvailability(
        IEnumerable<(ProductVariant Variant, int RequestedQuantity)> items)
    {
        Guard.Against.Null(items, nameof(items));

        var itemsList = items.ToList();
        var errors = new List<string>();

        foreach (var (variant, quantity) in itemsList)
        {
            if (variant == null) continue;

            if (!variant.IsActive || variant.IsDeleted)
            {
                errors.Add($"محصول با شناسه {variant.Id} غیرفعال یا حذف شده است.");
                continue;
            }

            if (!variant.IsUnlimited && variant.AvailableStock < quantity)
            {
                errors.Add($"موجودی محصول با شناسه {variant.Id} کافی نیست. " +
                           $"موجودی: {variant.AvailableStock}، درخواستی: {quantity}");
            }
        }

        return errors.Any()
            ? BatchReservationValidation.Failed(errors)
            : BatchReservationValidation.Valid();
    }

    /// <summary>
    /// رزرو دسته‌ای موجودی برای سفارش - روی هر Variant تغییر اعمال می‌کند
    /// </summary>
    public BatchReservationResult ReserveBatch(
        IEnumerable<(ProductVariant Variant, int Quantity, int OrderItemId)> items,
        int? userId = null,
        string? correlationId = null)
    {
        Guard.Against.Null(items, nameof(items));

        var itemsList = items.ToList();
        var transactions = new List<InventoryTransaction>();
        var errors = new List<string>();

        foreach (var (variant, quantity, orderItemId) in itemsList)
        {
            if (variant == null) continue;

            if (!variant.IsActive || variant.IsDeleted)
            {
                errors.Add($"واریانت {variant.Id} غیرفعال است.");
                continue;
            }

            if (variant.IsUnlimited)
            {
                var unlimitedTx = InventoryTransaction.CreateReservation(
                    variant.Id, quantity, variant.StockQuantity,
                    orderItemId, userId, correlationId: correlationId);
                transactions.Add(unlimitedTx);
                continue;
            }

            if (variant.AvailableStock < quantity)
            {
                errors.Add($"موجودی کافی نیست. واریانت: {variant.Id}, " +
                           $"موجودی: {variant.AvailableStock}, درخواستی: {quantity}");
                continue;
            }

            var transaction = InventoryTransaction.CreateReservation(
                variant.Id, quantity, variant.StockQuantity,
                orderItemId, userId, correlationId: correlationId);

            variant.Reserve(quantity);
            transactions.Add(transaction);
        }

        if (errors.Any())
            return BatchReservationResult.Failed(errors);

        return BatchReservationResult.Success(transactions);
    }

    /// <summary>
    /// آزادسازی دسته‌ای رزرو در صورت لغو سفارش
    /// </summary>
    public BatchReleaseResult ReleaseBatch(
        IEnumerable<(ProductVariant Variant, int Quantity)> items,
        int? userId = null,
        string? reason = null)
    {
        Guard.Against.Null(items, nameof(items));

        var transactions = new List<InventoryTransaction>();

        foreach (var (variant, quantity) in items)
        {
            if (variant == null || variant.IsUnlimited) continue;

            var actualRelease = Math.Min(quantity, variant.ReservedQuantity);
            if (actualRelease == 0) continue;

            variant.Release(actualRelease);

            var transaction = InventoryTransaction.Create(
                variant.Id,
                TransactionType.ReservationRollback,
                actualRelease,
                variant.StockQuantity - actualRelease,
                userId,
                reason ?? "آزادسازی رزرو");

            transactions.Add(transaction);
        }

        return BatchReleaseResult.Success(transactions);
    }
}

#region Result Types

public sealed class BatchReservationValidation
{
    public bool IsValid { get; private set; }
    public IReadOnlyList<string> Errors { get; private set; } = new List<string>();

    private BatchReservationValidation()
    { }

    public static BatchReservationValidation Valid() => new() { IsValid = true };

    public static BatchReservationValidation Failed(IEnumerable<string> errors) =>
        new() { IsValid = false, Errors = errors.ToList().AsReadOnly() };

    public string GetErrorsSummary() => string.Join(" | ", Errors);
}

public sealed class BatchReservationResult
{
    public bool IsSuccess { get; private set; }
    public IReadOnlyList<string> Errors { get; private set; } = new List<string>();
    public IReadOnlyList<InventoryTransaction> Transactions { get; private set; } = new List<InventoryTransaction>();

    private BatchReservationResult()
    { }

    public static BatchReservationResult Success(IEnumerable<InventoryTransaction> transactions) =>
        new() { IsSuccess = true, Transactions = transactions.ToList().AsReadOnly() };

    public static BatchReservationResult Failed(IEnumerable<string> errors) =>
        new() { IsSuccess = false, Errors = errors.ToList().AsReadOnly() };

    public string GetErrorsSummary() => string.Join(" | ", Errors);
}

public sealed class BatchReleaseResult
{
    public bool IsSuccess { get; private set; }
    public IReadOnlyList<InventoryTransaction> Transactions { get; private set; } = new List<InventoryTransaction>();

    private BatchReleaseResult()
    { }

    public static BatchReleaseResult Success(IEnumerable<InventoryTransaction> transactions) =>
        new() { IsSuccess = true, Transactions = transactions.ToList().AsReadOnly() };
}

#endregion Result Types