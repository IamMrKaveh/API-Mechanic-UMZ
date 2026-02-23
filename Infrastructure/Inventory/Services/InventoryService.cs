namespace Infrastructure.Inventory.Services;

public class InventoryService : IInventoryService
{
    private readonly Persistence.Context.DBContext _context;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly InventoryDomainService _domainService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        Persistence.Context.DBContext context,
        IInventoryRepository inventoryRepository,
        InventoryDomainService domainService,
        IUnitOfWork unitOfWork,
        ILogger<InventoryService> logger
        )
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
        string? correlationId = null,
        string? cartId = null,
        DateTime? expiresAt = null,
        CancellationToken ct = default
        )
    {
        return await ExecuteWithSerializableLockAsync(variantId, async variant =>
        {
            if (!string.IsNullOrEmpty(correlationId))
            {
                var existing = await _context.InventoryTransactions
                    .AnyAsync(t =>
                        t.VariantId == variantId &&
                        t.TransactionType == TransactionType.Reservation.Value &&
                        t.CorrelationId == correlationId &&
                        !t.IsReversed, ct);

                if (existing)
                {
                    _logger.LogWarning(
                        "Duplicate reserve request for variant {VariantId} with correlation {CorrelationId}. Skipping.",
                        variantId, correlationId);
                    return ServiceResult.Success();
                }
            }

            var result = _domainService.Reserve(
                variant, quantity, orderItemId, userId, referenceNumber,
                correlationId, cartId, expiresAt);

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
        string? correlationId = null,
        CancellationToken ct = default
        )
    {
        return await ExecuteWithSerializableLockAsync(variantId, async variant =>
        {
            if (!string.IsNullOrEmpty(correlationId))
            {
                var existing = await _context.InventoryTransactions
                    .AnyAsync(t =>
                        t.VariantId == variantId &&
                        t.TransactionType == TransactionType.Commit.Value &&
                        t.CorrelationId == correlationId, ct);

                if (existing)
                {
                    _logger.LogWarning(
                        "Duplicate commit for variant {VariantId} with correlation {CorrelationId}. Skipping.",
                        variantId, correlationId);
                    return ServiceResult.Success();
                }
            }

            var result = _domainService.ConfirmReservation(
                variant, quantity, orderItemId, userId, referenceNumber, correlationId);

            if (!result.IsSuccess)
                return ServiceResult.Failure(result.Error!);

            if (result.Transaction is not null)
                await _inventoryRepository.AddTransactionAsync(result.Transaction, ct);

            return ServiceResult.Success();
        }, ct);
    }

    public async Task<ServiceResult> CommitStockForOrderAsync(
        int orderId,
        CancellationToken ct = default
        )
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);

            try
            {
                var orderItems = await _context.Set<Domain.Order.OrderItem>()
                    .Where(oi => oi.OrderId == orderId)
                    .ToListAsync(ct);

                if (!orderItems.Any())
                {
                    await transaction.RollbackAsync(ct);
                    return ServiceResult.Failure("آیتمی برای سفارش یافت نشد.");
                }

                foreach (var item in orderItems)
                {
                    var correlationId = $"COMMIT-ORDER-{orderId}-ITEM-{item.Id}";
                    var referenceNumber = $"ORDER-{orderId}";

                    var variant = await _inventoryRepository.GetVariantWithLockAsync(item.VariantId, ct);

                    if (variant == null)
                    {
                        _logger.LogWarning(
                            "Variant {VariantId} not found while committing order {OrderId}",
                            item.VariantId, orderId);
                        continue;
                    }

                    var alreadyCommitted = await _context.InventoryTransactions
                        .AnyAsync(t =>
                            t.VariantId == item.VariantId &&
                            t.TransactionType == TransactionType.Commit.Value &&
                            t.CorrelationId == correlationId, ct);

                    if (alreadyCommitted) continue;

                    var result = _domainService.ConfirmReservation(
                        variant, item.Quantity, item.Id, null, referenceNumber, correlationId);

                    if (!result.IsSuccess)
                    {
                        _logger.LogWarning(
                            "Could not confirm reservation for variant {VariantId} in order {OrderId}: {Error}",
                            item.VariantId, orderId, result.Error);
                        continue;
                    }

                    if (result.Transaction is not null)
                        await _inventoryRepository.AddTransactionAsync(result.Transaction, ct);
                }

                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Committed inventory for all items of Order {OrderId}", orderId);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to commit stock for order {OrderId}", orderId);
                return ServiceResult.Failure("خطا در Commit موجودی سفارش.");
            }
        });
    }

    public async Task<ServiceResult> RollbackReservationAsync(
        int variantId,
        int quantity,
        int? userId = null,
        string? reason = null,
        CancellationToken ct = default
        )
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

    public async Task<ServiceResult> RollbackReservationsAsync(
        string referenceNumber,
        CancellationToken ct = default
        )
    {
        var transactions = await _context.InventoryTransactions
            .Where(t => t.ReferenceNumber == referenceNumber &&
                        t.TransactionType == TransactionType.Reservation.Value &&
                        !t.IsReversed)
            .ToListAsync(ct);

        foreach (var tx in transactions)
        {
            var variant = await _context.Set<ProductVariant>()
                .FindAsync(new object[] { tx.VariantId }, ct);

            if (variant != null && !variant.IsUnlimited)
            {
                variant.Release(Math.Abs(tx.QuantityChange));
                tx.MarkAsReversed();

                var rollbackTx = InventoryTransaction.Create(
                    tx.VariantId,
                    TransactionType.ReservationRollback,
                    Math.Abs(tx.QuantityChange),
                    variant.StockQuantity - Math.Abs(tx.QuantityChange),
                    notes: $"آزادسازی خودکار رزرو - مرجع: {referenceNumber}",
                    referenceNumber: referenceNumber);

                await _inventoryRepository.AddTransactionAsync(rollbackTx, ct);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ReturnStockAsync(
        int variantId,
        int quantity,
        int orderId,
        int orderItemId,
        int userId,
        string reason,
        CancellationToken ct = default
        )
    {
        return await ExecuteWithSerializableLockAsync(variantId, async variant =>
        {
            var result = _domainService.ReturnStock(variant, quantity, orderId, orderItemId, userId, reason);

            if (!result.IsSuccess)
                return ServiceResult.Failure(result.Error!);

            if (result.Transaction is not null)
                await _inventoryRepository.AddTransactionAsync(result.Transaction, ct);

            _logger.LogInformation(
                "Stock returned for variant {VariantId}: +{Quantity} (Order {OrderId})",
                variantId, quantity, orderId);

            return ServiceResult.Success();
        }, ct);
    }

    public async Task<ServiceResult> ReturnStockForOrderAsync(
        int orderId,
        int userId,
        string reason,
        CancellationToken ct = default
        )
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);

            try
            {
                var orderItems = await _context.Set<Domain.Order.OrderItem>()
                    .Where(oi => oi.OrderId == orderId)
                    .ToListAsync(ct);

                foreach (var item in orderItems)
                {
                    var variant = await _context.Set<ProductVariant>()
                        .FromSqlRaw("SELECT * FROM \"ProductVariants\" WHERE \"Id\" = {0} FOR UPDATE", item.VariantId)
                        .Include(v => v.Product)
                        .FirstOrDefaultAsync(ct);

                    if (variant == null || variant.IsUnlimited) continue;

                    var result = _domainService.ReturnStock(
                        variant, item.Quantity, orderId, item.Id, userId, reason);

                    if (!result.IsSuccess)
                    {
                        _logger.LogWarning(
                            "Return stock failed for variant {VariantId}: {Error}",
                            item.VariantId, result.Error);
                        continue;
                    }

                    if (result.Transaction is not null)
                        await _inventoryRepository.AddTransactionAsync(result.Transaction, ct);
                }

                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Stock returned for all items of Order {OrderId}", orderId);
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                _logger.LogError(ex, "Failed to return stock for order {OrderId}", orderId);
                return ServiceResult.Failure("خطا در مرجوعی موجودی سفارش.");
            }
        });
    }

    public async Task<ServiceResult> AdjustStockAsync(
        int variantId,
        int quantityChange,
        int userId,
        string notes,
        CancellationToken ct = default
        )
    {
        try
        {
            var variant = await GetVariantWithTrackingAsync(variantId, ct);
            if (variant is null)
                return ServiceResult.Failure("واریانت مورد نظر یافت نشد.");

            var result = _domainService.AdjustStock(variant, quantityChange, userId, notes);
            if (!result.IsSuccess) return ServiceResult.Failure(result.Error!);

            if (result.Transaction is not null)
                await _inventoryRepository.AddTransactionAsync(result.Transaction, ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Failure("تداخل همزمانی - لطفاً دوباره تلاش کنید.");
        }
    }

    public async Task<ServiceResult> RecordDamageAsync(
        int variantId,
        int quantity,
        int userId,
        string notes,
        CancellationToken ct = default
        )
    {
        try
        {
            var variant = await GetVariantWithTrackingAsync(variantId, ct);
            if (variant is null) return ServiceResult.Failure("واریانت مورد نظر یافت نشد.");

            var result = _domainService.RecordDamage(variant, quantity, userId, notes);
            if (!result.IsSuccess) return ServiceResult.Failure(result.Error!);

            if (result.Transaction is not null)
                await _inventoryRepository.AddTransactionAsync(result.Transaction, ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Failure("تداخل همزمانی - لطفاً دوباره تلاش کنید.");
        }
    }

    public async Task<ServiceResult<(int VariantId, int FinalStock, int Difference, bool HasDiscrepancy, string Message)>> ReconcileStockAsync(
        int variantId,
        int userId,
        CancellationToken ct = default
        )
    {
        return await ExecuteWithSerializableLockAsync<ServiceResult<(int, int, int, bool, string)>>(variantId,
            async variant =>
            {
                var calculatedStock = await _inventoryRepository.CalculateStockFromTransactionsAsync(variantId, ct);
                var result = _domainService.Reconcile(variant, calculatedStock, userId);

                if (!result.IsSuccess)
                    return ServiceResult<(int, int, int, bool, string)>.Failure(result.Message!);

                if (result.Transaction is not null)
                    await _inventoryRepository.AddTransactionAsync(result.Transaction, ct);

                return ServiceResult<(int, int, int, bool, string)>.Success((
                    result.VariantId,
                    result.FinalStock,
                    result.Difference,
                    result.HasDiscrepancy,
                    result.Message ?? string.Empty
                ));
            }, ct);
    }

    public async Task<ServiceResult<(int Total, int Success, int Failed, IEnumerable<(int VariantId, bool IsSuccess, string? Error, int? NewStock)> Results)>> BulkAdjustStockAsync(
        IEnumerable<(int VariantId, int QuantityChange, string Notes)> items,
        int userId,
        CancellationToken ct = default
        )
    {
        var itemsList = items.ToList();
        var results = new List<(int, bool, string?, int?)>();
        var successCount = 0; var failedCount = 0;

        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);

        try
        {
            foreach (var item in itemsList)
            {
                try
                {
                    var variant = await GetVariantWithTrackingAsync(item.VariantId, ct);
                    if (variant is null)
                    {
                        results.Add((item.VariantId, false, "واریانت یافت نشد.", null));
                        failedCount++; continue;
                    }

                    var adjustResult = _domainService.AdjustStock(variant, item.QuantityChange, userId, item.Notes);
                    if (!adjustResult.IsSuccess)
                    {
                        results.Add((item.VariantId, false, adjustResult.Error, null));
                        failedCount++; continue;
                    }

                    if (adjustResult.Transaction is not null)
                        await _inventoryRepository.AddTransactionAsync(adjustResult.Transaction, ct);

                    results.Add((item.VariantId, true, null, adjustResult.NewStock));
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adjusting stock for variant {VariantId}", item.VariantId);
                    results.Add((item.VariantId, false, ex.Message, null));
                    failedCount++;
                }
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return ServiceResult<(int, int, int, IEnumerable<(int, bool, string?, int?)>)>.Success((
                itemsList.Count,
                successCount,
                failedCount,
                results
            ));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Bulk stock adjustment failed.");
            return ServiceResult<(int, int, int, IEnumerable<(int, bool, string?, int?)>)>.Failure("خطا در تنظیم دسته‌ای موجودی.");
        }
    }

    public async Task<ServiceResult<(int Total, int Success, int Failed, IEnumerable<(int VariantId, bool IsSuccess, string? Error, int? NewStock)> Results)>> BulkStockInAsync(
        IEnumerable<(int VariantId, int Quantity, string? Notes)> items,
        int userId,
        string? supplierReference = null,
        CancellationToken ct = default
        )
    {
        var itemsList = items.ToList();
        var results = new List<(int, bool, string?, int?)>();
        var successCount = 0; var failedCount = 0;

        await using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);

        try
        {
            foreach (var item in itemsList)
            {
                try
                {
                    var variant = await GetVariantWithTrackingAsync(item.VariantId, ct);
                    if (variant is null)
                    {
                        results.Add((item.VariantId, false, "واریانت یافت نشد.", null));
                        failedCount++; continue;
                    }

                    var correlationId = $"STOCKIN-{supplierReference}-VARIANT-{item.VariantId}-{DateTime.UtcNow:yyyyMMddHHmmss}";

                    if (!string.IsNullOrEmpty(supplierReference))
                    {
                        var alreadyProcessed = await _context.InventoryTransactions
                            .AnyAsync(t =>
                                t.VariantId == item.VariantId &&
                                t.TransactionType == TransactionType.StockIn.Value &&
                                t.CorrelationId == correlationId, ct);

                        if (alreadyProcessed)
                        {
                            results.Add((item.VariantId, true, null, variant.StockQuantity));
                            successCount++; continue;
                        }
                    }

                    variant.AddStock(item.Quantity);

                    var tx = InventoryTransaction.CreateStockIn(
                        item.VariantId,
                        item.Quantity,
                        variant.StockQuantity - item.Quantity,
                        userId,
                        item.Notes ?? "ورود موجودی از تأمین‌کننده",
                        supplierReference,
                        correlationId: correlationId);

                    await _inventoryRepository.AddTransactionAsync(tx, ct);
                    results.Add((item.VariantId, true, null, variant.StockQuantity));
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in StockIn for variant {VariantId}", item.VariantId);
                    results.Add((item.VariantId, false, ex.Message, null));
                    failedCount++;
                }
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return ServiceResult<(int, int, int, IEnumerable<(int, bool, string?, int?)>)>.Success((
                itemsList.Count,
                successCount,
                failedCount,
                results
            ));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Bulk StockIn failed.");
            return ServiceResult<(int, int, int, IEnumerable<(int, bool, string?, int?)>)>.Failure("خطا در Import دسته‌ای موجودی.");
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
        CancellationToken ct = default
        )
    {
        var variant = await GetVariantWithTrackingAsync(variantId, ct);
        var currentStock = stockBefore ?? variant?.StockQuantity ?? 0;
        var txType = TransactionType.FromString(transactionType);

        var transaction = InventoryTransaction.Create(
            variantId, txType, quantityChange, currentStock, userId, notes, referenceNumber, orderItemId);

        await _inventoryRepository.AddTransactionAsync(transaction, ct);
        variant?.AdjustStock(quantityChange);

        if (saveChanges) await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<ServiceResult> ExecuteWithSerializableLockAsync(
        int variantId,
        Func<ProductVariant, Task<ServiceResult>> operation,
        CancellationToken ct
        )
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);

            try
            {
                var variant = await _context.Set<ProductVariant>()
                    .FromSqlRaw("SELECT * FROM \"ProductVariants\" WHERE \"Id\" = {0} FOR UPDATE", variantId)
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
                _logger.LogError(ex, "Error in serializable lock for variant {VariantId}", variantId);
                return ServiceResult.Failure("خطا در عملیات موجودی. لطفاً دوباره تلاش کنید.");
            }
        });
    }

    private async Task<TResult> ExecuteWithSerializableLockAsync<TResult>(
        int variantId,
        Func<ProductVariant, Task<TResult>> operation,
        CancellationToken ct
        ) where TResult : class
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);

            try
            {
                var variant = await _context.Set<ProductVariant>()
                    .FromSqlRaw("SELECT * FROM \"ProductVariants\" WHERE \"Id\" = {0} FOR UPDATE", variantId)
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(ct);

                if (variant is null)
                {
                    await transaction.RollbackAsync(ct);
                    if (typeof(TResult) == typeof(ServiceResult))
                        return (TResult)(object)ServiceResult.Failure("واریانت مورد نظر یافت نشد.");

                    var failMethod = typeof(TResult).GetMethod("Failure", new[] { typeof(string) });
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
                _logger.LogError(ex, "Error in serializable lock for variant {VariantId}", variantId);
                throw;
            }
        });
    }

    private async Task<ProductVariant?> GetVariantWithTrackingAsync(
        int variantId,
        CancellationToken ct
        )
    {
        return await _context.Set<ProductVariant>()
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId, ct);
    }

    public Task ReconcileAsync(
        int variantId,
        int physicalCount,
        string reason,
        int userId,
        int? warehouseId = null,
        CancellationToken ct = default
        )
    {
        throw new NotImplementedException();
    }
}