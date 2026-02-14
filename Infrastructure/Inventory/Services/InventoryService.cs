namespace Infrastructure.Inventory.Services;

/// <summary>
/// Application-level Inventory Service Implementation
/// هماهنگی بین Domain Service، Repository و UnitOfWork
/// مدیریت همزمانی با Pessimistic Locking برای رزرو و Optimistic Concurrency برای تنظیم دستی
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly LedkaContext _context;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly InventoryDomainService _domainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        LedkaContext context,
        IInventoryRepository inventoryRepository,
        InventoryDomainService domainService,
        IUnitOfWork unitOfWork,
        ILogger<InventoryService> logger)
    {
        _context = context;
        _inventoryRepository = inventoryRepository;
        _domainService = domainService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult> ReserveStockAsync(
        int variantId,
        int quantity,
        int orderItemId,
        int? userId = null,
        string? referenceNumber = null,
        CancellationToken ct = default)
    {
        return await ExecuteWithSerializableLockAsync(variantId, async variant =>
        {
            var result = _domainService.Reserve(variant, quantity, orderItemId, userId, referenceNumber);

            if (!result.IsSuccess)
                return ServiceResult.Failure(result.Error!);

            if (result.Transaction is not null)
                await _inventoryRepository.AddTransactionAsync(result.Transaction, ct);

            return ServiceResult.Success();
        }, ct);
    }

    public async Task<ServiceResult> ConfirmReservationAsync(
        int variantId,
        int quantity,
        int orderItemId,
        int? userId = null,
        string? referenceNumber = null,
        CancellationToken ct = default)
    {
        return await ExecuteWithSerializableLockAsync(variantId, async variant =>
        {
            var result = _domainService.ConfirmReservation(variant, quantity, orderItemId, userId, referenceNumber);

            if (!result.IsSuccess)
                return ServiceResult.Failure(result.Error!);

            if (result.Transaction is not null)
                await _inventoryRepository.AddTransactionAsync(result.Transaction, ct);

            return ServiceResult.Success();
        }, ct);
    }

    public async Task<ServiceResult> RollbackReservationAsync(
        int variantId,
        int quantity,
        int? userId = null,
        string? reason = null,
        CancellationToken ct = default)
    {
        return await ExecuteWithSerializableLockAsync(variantId, async variant =>
        {
            var result = _domainService.RollbackReservation(variant, quantity, userId, reason);

            if (!result.IsSuccess)
                return ServiceResult.Failure(result.Message ?? "خطا در آزادسازی موجودی.");

            if (result.Transaction is not null)
                await _inventoryRepository.AddTransactionAsync(result.Transaction, ct);

            return ServiceResult.Success();
        }, ct);
    }

    public async Task<ServiceResult> AdjustStockAsync(
        int variantId,
        int quantityChange,
        int userId,
        string notes,
        CancellationToken ct = default)
    {
        try
        {
            // Optimistic Concurrency - از RowVersion واریانت استفاده می‌شود
            var variant = await GetVariantWithTrackingAsync(variantId, ct);
            if (variant is null)
                return ServiceResult.Failure("واریانت مورد نظر یافت نشد.");

            var result = _domainService.AdjustStock(variant, quantityChange, userId, notes);

            if (!result.IsSuccess)
                return ServiceResult.Failure(result.Error!);

            if (result.Transaction is not null)
                await _inventoryRepository.AddTransactionAsync(result.Transaction, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Stock adjusted for variant {VariantId}: {Change:+#;-#;0}. New stock: {NewStock}",
                variantId, quantityChange, result.NewStock);

            return ServiceResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Concurrency conflict adjusting stock for variant {VariantId}. Retrying...",
                variantId);
            return ServiceResult.Failure("تداخل همزمانی - لطفاً دوباره تلاش کنید.");
        }
    }

    public async Task<ServiceResult> RecordDamageAsync(
        int variantId,
        int quantity,
        int userId,
        string notes,
        CancellationToken ct = default)
    {
        try
        {
            var variant = await GetVariantWithTrackingAsync(variantId, ct);
            if (variant is null)
                return ServiceResult.Failure("واریانت مورد نظر یافت نشد.");

            var result = _domainService.RecordDamage(variant, quantity, userId, notes);

            if (!result.IsSuccess)
                return ServiceResult.Failure(result.Error!);

            if (result.Transaction is not null)
                await _inventoryRepository.AddTransactionAsync(result.Transaction, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Damage recorded for variant {VariantId}: {Quantity} units. New stock: {NewStock}",
                variantId, quantity, result.NewStock);

            return ServiceResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning(
                "Concurrency conflict recording damage for variant {VariantId}.",
                variantId);
            return ServiceResult.Failure("تداخل همزمانی - لطفاً دوباره تلاش کنید.");
        }
    }

    public async Task<ServiceResult<ReconcileResultDto>> ReconcileStockAsync(
        int variantId,
        int userId,
        CancellationToken ct = default)
    {
        return await ExecuteWithSerializableLockAsync<ServiceResult<ReconcileResultDto>>(
            variantId,
            async variant =>
            {
                var calculatedStock = await _inventoryRepository.CalculateStockFromTransactionsAsync(variantId, ct);

                var result = _domainService.Reconcile(variant, calculatedStock, userId);

                if (!result.IsSuccess)
                    return ServiceResult<ReconcileResultDto>.Failure(result.Message!);

                if (result.Transaction is not null)
                    await _inventoryRepository.AddTransactionAsync(result.Transaction, ct);

                var dto = new ReconcileResultDto
                {
                    VariantId = result.VariantId,
                    FinalStock = result.FinalStock,
                    Difference = result.Difference,
                    HasDiscrepancy = result.HasDiscrepancy,
                    Message = result.Message ?? string.Empty
                };

                return ServiceResult<ReconcileResultDto>.Success(dto);
            }, ct);
    }

    public async Task<ServiceResult<BulkAdjustResultDto>> BulkAdjustStockAsync(
        IEnumerable<BulkAdjustItemDto> items,
        int userId,
        CancellationToken ct = default)
    {
        var itemsList = items.ToList();
        var results = new List<BulkAdjustItemResultDto>();
        var successCount = 0;
        var failedCount = 0;

        // استفاده از Transaction برای تمام عملیات دسته‌ای
        await using var transaction = await _context.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, ct);

        try
        {
            foreach (var item in itemsList)
            {
                try
                {
                    var variant = await GetVariantWithTrackingAsync(item.VariantId, ct);
                    if (variant is null)
                    {
                        results.Add(new BulkAdjustItemResultDto
                        {
                            VariantId = item.VariantId,
                            IsSuccess = false,
                            Error = "واریانت یافت نشد."
                        });
                        failedCount++;
                        continue;
                    }

                    var adjustResult = _domainService.AdjustStock(variant, item.QuantityChange, userId, item.Notes);

                    if (!adjustResult.IsSuccess)
                    {
                        results.Add(new BulkAdjustItemResultDto
                        {
                            VariantId = item.VariantId,
                            IsSuccess = false,
                            Error = adjustResult.Error
                        });
                        failedCount++;
                        continue;
                    }

                    if (adjustResult.Transaction is not null)
                        await _inventoryRepository.AddTransactionAsync(adjustResult.Transaction, ct);

                    results.Add(new BulkAdjustItemResultDto
                    {
                        VariantId = item.VariantId,
                        IsSuccess = true,
                        NewStock = adjustResult.NewStock
                    });
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adjusting stock for variant {VariantId}", item.VariantId);
                    results.Add(new BulkAdjustItemResultDto
                    {
                        VariantId = item.VariantId,
                        IsSuccess = false,
                        Error = ex.Message
                    });
                    failedCount++;
                }
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            var resultDto = new BulkAdjustResultDto
            {
                TotalRequested = itemsList.Count,
                SuccessCount = successCount,
                FailedCount = failedCount,
                Results = results
            };

            _logger.LogInformation(
                "Bulk stock adjustment completed: {Success}/{Total} successful",
                successCount, itemsList.Count);

            return ServiceResult<BulkAdjustResultDto>.Success(resultDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Bulk stock adjustment failed. Rolling back all changes.");
            return ServiceResult<BulkAdjustResultDto>.Failure("خطا در تنظیم دسته‌ای موجودی. تمام تغییرات بازگردانی شد.");
        }
    }

    public async Task LogTransactionAsync(
        int variantId,
        string transactionType,
        int quantityChange,
        int? orderItemId,
        int? userId,
        string? notes = null,
        string? referenceNumber = null,
        int? stockBefore = null,
        bool saveChanges = true,
        CancellationToken ct = default)
    {
        var variant = await GetVariantWithTrackingAsync(variantId, ct);
        var currentStock = stockBefore ?? variant?.StockQuantity ?? 0;

        var txType = TransactionType.FromString(transactionType);

        var transaction = InventoryTransaction.Create(
            variantId,
            txType,
            quantityChange,
            currentStock,
            userId,
            notes,
            referenceNumber,
            orderItemId);

        await _inventoryRepository.AddTransactionAsync(transaction, ct);

        // به‌روزرسانی موجودی واریانت
        variant?.AdjustStock(quantityChange);

        if (saveChanges)
        {
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    #region Private Helpers

    /// <summary>
    /// اجرای عملیات با Pessimistic Locking (Serializable Isolation)
    /// برای جلوگیری از Over-selling در عملیات رزرو
    /// </summary>
    private async Task<ServiceResult> ExecuteWithSerializableLockAsync(
        int variantId,
        Func<ProductVariant, Task<ServiceResult>> operation,
        CancellationToken ct)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, ct);

            try
            {
                // Row-level lock با SELECT ... FOR UPDATE (PostgreSQL)
                var variant = await _context.Set<ProductVariant>()
                    .FromSqlRaw(
                        "SELECT * FROM \"ProductVariants\" WHERE \"Id\" = {0} FOR UPDATE",
                        variantId)
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(ct);

                if (variant is null)
                {
                    await transaction.RollbackAsync(ct);
                    return ServiceResult.Failure("واریانت مورد نظر یافت نشد.");
                }

                var result = await operation(variant);

                if (result.IsSucceed)
                {
                    await _context.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                }
                else
                {
                    await transaction.RollbackAsync(ct);
                }

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error in serializable lock operation for variant {VariantId}", variantId);
                return ServiceResult.Failure("خطا در عملیات موجودی. لطفاً دوباره تلاش کنید.");
            }
        });
    }

    /// <summary>
    /// اجرای عملیات با Pessimistic Locking - نسخه Generic
    /// </summary>
    private async Task<TResult> ExecuteWithSerializableLockAsync<TResult>(
        int variantId,
        Func<ProductVariant, Task<TResult>> operation,
        CancellationToken ct)
        where TResult : class
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, ct);

            try
            {
                var variant = await _context.Set<ProductVariant>()
                    .FromSqlRaw(
                        "SELECT * FROM \"ProductVariants\" WHERE \"Id\" = {0} FOR UPDATE",
                        variantId)
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(ct);

                if (variant is null)
                {
                    await transaction.RollbackAsync(ct);

                    // تولید نتیجه خطا بر اساس نوع بازگشتی
                    if (typeof(TResult) == typeof(ServiceResult))
                        return (TResult)(object)ServiceResult.Failure("واریانت مورد نظر یافت نشد.");

                    // برای ServiceResult<T>
                    var failMethod = typeof(TResult).GetMethod("Fail", new[] { typeof(string) });
                    if (failMethod is not null)
                        return (TResult)failMethod.Invoke(null, new object[] { "واریانت مورد نظر یافت نشد." })!;

                    throw new InvalidOperationException("واریانت مورد نظر یافت نشد.");
                }

                var result = await operation(variant);

                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Error in serializable lock operation for variant {VariantId}", variantId);
                throw;
            }
        });
    }

    private async Task<ProductVariant?> GetVariantWithTrackingAsync(int variantId, CancellationToken ct)
    {
        return await _context.Set<ProductVariant>()
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId, ct);
    }

    public Task<ServiceResult> RollbackReservationsAsync(string referenceNumber, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    #endregion Private Helpers
}