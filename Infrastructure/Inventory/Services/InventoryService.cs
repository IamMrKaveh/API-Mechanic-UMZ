using Application.Common.Exceptions;
using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Order.Entities;
using Domain.Variant.Aggregates;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Inventory.Services;

public class InventoryService(
    DBContext context,
    IInventoryRepository inventoryRepository,
    InventoryDomainService domainService,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IInventoryService
{
    public async Task<ServiceResult> ReserveStockAsync(
        int variantId,
        int quantity,
        int orderItemId,
        int? userId = null,
        string? referenceNumber = null,
        string? correlationId = null,
        string? cartId = null,
        DateTime? expiresAt = null,
        CancellationToken ct = default)
    {
        return await ExecuteWithSerializableLockAsync(variantId, async variant =>
        {
            if (!string.IsNullOrEmpty(correlationId))
            {
                var existing = await context.InventoryTransactions
                    .AnyAsync(t =>
                        t.VariantId == variantId &&
                        t.TransactionType == TransactionType.Reservation.Value &&
                        t.CorrelationId == correlationId &&
                        !t.IsReversed, ct);

                if (existing)
                {
                    await auditService.LogWarningAsync(
                        "Duplicate reserve request for variant {VariantId} with correlation {CorrelationId}. Skipping.",
                        ct);
                    return ServiceResult.Success();
                }
            }

            var result = domainService.Reserve(
                variant, quantity, orderItemId, userId, referenceNumber,
                correlationId, cartId, expiresAt);

            if (!result.IsSuccess)
                return ServiceResult.Failure(result.Error.Message!);

            if (result.Transaction is not null)
                await inventoryRepository.AddTransactionAsync(result.Transaction, ct);

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
        CancellationToken ct = default)
    {
        return await ExecuteWithSerializableLockAsync(variantId, async variant =>
        {
            if (!string.IsNullOrEmpty(correlationId))
            {
                var existing = await context.InventoryTransactions
                    .AnyAsync(t =>
                        t.VariantId == variantId &&
                        t.TransactionType == TransactionType.Commit.Value &&
                        t.CorrelationId == correlationId, ct);

                if (existing)
                {
                    await auditService.LogWarningAsync(
                        "Duplicate commit for variant {VariantId} with correlation {CorrelationId}. Skipping.",
                        ct);
                    return ServiceResult.Success();
                }
            }

            var result = domainService.ConfirmReservation(
                variant, quantity, orderItemId, userId, referenceNumber, correlationId);

            if (!result.IsSuccess)
                return ServiceResult.Failure(result.Error.Message);

            if (result.Transaction is not null)
                await inventoryRepository.AddTransactionAsync(result.Transaction, ct);

            return ServiceResult.Success();
        }, ct);
    }

    public async Task<ServiceResult> CommitStockForOrderAsync(
        int orderId,
        CancellationToken ct = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);

            try
            {
                var orderItems = await context.Set<OrderItem>()
                    .Where(oi => oi.OrderId == orderId)
                    .ToListAsync(ct);

                if (!orderItems.Any())
                {
                    await transaction.RollbackAsync(ct);
                    return ServiceResult.NotFound("آیتمی برای سفارش یافت نشد.");
                }

                foreach (var item in orderItems)
                {
                    var correlationId = $"COMMIT-ORDER-{orderId}-ITEM-{item.Id}";
                    var referenceNumber = $"ORDER-{orderId}";

                    var variant = await inventoryRepository.GetVariantWithLockAsync(item.VariantId, ct);

                    if (variant == null)
                    {
                        await auditService.LogWarningAsync(
                            "Variant {VariantId} not found while committing order {OrderId}",
                            ct);
                        continue;
                    }

                    var alreadyCommitted = await context.InventoryTransactions
                        .AnyAsync(t =>
                            t.VariantId == item.VariantId &&
                            t.TransactionType == TransactionType.Commit.Value &&
                            t.CorrelationId == correlationId, ct);

                    if (alreadyCommitted) continue;

                    var result = domainService.ConfirmReservation(
                        variant, item.Quantity, item.Id, null, referenceNumber, correlationId);

                    if (!result.IsSuccess)
                    {
                        await auditService.LogWarningAsync(
                            "Could not confirm reservation for variant {VariantId} in order {OrderId}: {Error}",
                            ct);
                        continue;
                    }

                    if (result.Transaction is not null)
                        await inventoryRepository.AddTransactionAsync(result.Transaction, ct);
                }

                await context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                await auditService.LogInformationAsync("Committed inventory for all items of Order {OrderId}", ct);
                return ServiceResult.Success();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(ct);
                await auditService.LogErrorAsync("Failed to commit stock for order {OrderId}", ct);
                return ServiceResult.Failure("خطا در Commit موجودی سفارش.");
            }
        });
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
            var result = domainService.RollbackReservation(variant, quantity, userId, reason);

            if (!result.IsSuccess)
                return ServiceResult.Failure(result.Error.Message ?? "خطا در آزادسازی موجودی.");

            if (result.Transaction is not null)
                await inventoryRepository.AddTransactionAsync(result.Transaction, ct);

            return ServiceResult.Success();
        }, ct);
    }

    public async Task<ServiceResult> RollbackReservationsAsync(
        string referenceNumber,
        CancellationToken ct = default)
    {
        var transactions = await context.InventoryTransactions
            .Where(t => t.ReferenceNumber == referenceNumber &&
                        t.TransactionType == TransactionType.Reservation.Value &&
                        !t.IsReversed)
            .ToListAsync(ct);

        foreach (var tx in transactions)
        {
            var variant = await context.Set<ProductVariant>()
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

                await inventoryRepository.AddTransactionAsync(rollbackTx, ct);
            }
        }

        await unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ReturnStockAsync(
        int variantId,
        int quantity,
        int orderId,
        int orderItemId,
        int userId,
        string reason,
        CancellationToken ct = default)
    {
        return await ExecuteWithSerializableLockAsync(variantId, async variant =>
        {
            var result = domainService.ReturnStock(variant, quantity, orderId, orderItemId, userId, reason);

            if (!result.IsSuccess)
                return ServiceResult.Failure(result.Error.Message!);

            if (result.Transaction is not null)
                await inventoryRepository.AddTransactionAsync(result.Transaction, ct);

            await auditService.LogInformationAsync(
                "Stock returned for variant {VariantId}: +{Quantity} (Order {OrderId})",
                ct);

            return ServiceResult.Success();
        }, ct);
    }

    public async Task<ServiceResult> ReturnStockForOrderAsync(
        int orderId,
        int userId,
        string reason,
        CancellationToken ct = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);

            try
            {
                var orderItems = await context.Set<OrderItem>()
                    .Where(oi => oi.OrderId == orderId)
                    .ToListAsync(ct);

                foreach (var item in orderItems)
                {
                    var variant = await context.Set<ProductVariant>()
                        .FromSqlRaw("SELECT * FROM \"ProductVariants\" WHERE \"Id\" = {0} FOR UPDATE", item.VariantId)
                        .Include(v => v.Product)
                        .FirstOrDefaultAsync(ct);

                    if (variant == null || variant.IsUnlimited) continue;

                    var result = domainService.ReturnStock(
                        variant, item.Quantity, orderId, item.Id, userId, reason);

                    if (!result.IsSuccess)
                    {
                        await auditService.LogWarningAsync(
                            "Return stock failed for variant {VariantId}: {Error}",
                            ct);
                        continue;
                    }

                    if (result.Transaction is not null)
                        await inventoryRepository.AddTransactionAsync(result.Transaction, ct);
                }

                await context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                await auditService.LogInformationAsync("Stock returned for all items of Order {OrderId}", ct);
                return ServiceResult.Success();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(ct);
                await auditService.LogErrorAsync("Failed to return stock for order {OrderId}", ct);
                return ServiceResult.Failure("خطا در مرجوعی موجودی سفارش.");
            }
        });
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
            var variant = await GetVariantWithTrackingAsync(variantId, ct);
            if (variant is null)
                return ServiceResult.Failure("واریانت مورد نظر یافت نشد.");

            var result = domainService.AdjustStock(variant, quantityChange, userId, notes);
            if (!result.IsSuccess) return ServiceResult.Unexpected(result.Error!);

            if (result.Transaction is not null)
                await inventoryRepository.AddTransactionAsync(result.Transaction, ct);

            await unitOfWork.SaveChangesAsync(ct);
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
        CancellationToken ct = default)
    {
        try
        {
            var variant = await GetVariantWithTrackingAsync(variantId, ct);
            if (variant is null) return ServiceResult.Failure("واریانت مورد نظر یافت نشد.");

            var result = domainService.RecordDamage(variant, quantity, userId, notes);
            if (result.IsFailure)
                return ServiceResult.Failure(result.Error!);

            if (result.Transaction is not null)
                await inventoryRepository.AddTransactionAsync(result.Transaction, ct);

            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Failure("تداخل همزمانی - لطفاً دوباره تلاش کنید.");
        }
    }

    public async Task<ServiceResult<(VariantId VariantId, int FinalStock, int Difference, bool HasDiscrepancy, string Message)>> ReconcileStockAsync(
        int variantId,
        int userId,
        CancellationToken ct = default)
    {
        return await ExecuteWithSerializableLockAsync<ServiceResult<(int, int, int, bool, string)>>(variantId,
            async variant =>
            {
                var calculatedStock = await inventoryRepository.CalculateStockFromTransactionsAsync(variantId, ct);
                var result = domainService.Reconcile(variant, calculatedStock, userId);

                if (result.IsFailure)
                    return ServiceResult<(VariantId, int, int, bool, string)>
                    .Validation(result.Error.Message);

                if (result.Transaction is not null)
                    await inventoryRepository.AddTransactionAsync(result.Transaction, ct);

                return ServiceResult<(VariantId, int, int, bool, string)>
                .Success((
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
        CancellationToken ct = default)
    {
        var itemsList = items.ToList();
        var results = new List<(int, bool, string?, int?)>();
        var successCount = 0; var failedCount = 0;

        await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);

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

                    var adjustResult = domainService.AdjustStock(variant, item.QuantityChange, userId, item.Notes);
                    if (!adjustResult.IsSuccess)
                    {
                        results.Add((item.VariantId, false, adjustResult.Error, null));
                        failedCount++; continue;
                    }

                    if (adjustResult.Transaction is not null)
                        await inventoryRepository.AddTransactionAsync(adjustResult.Transaction, ct);

                    results.Add((item.VariantId, true, null, adjustResult.NewStock));
                    successCount++;
                }
                catch (Exception ex)
                {
                    await auditService.LogErrorAsync("Error adjusting stock for variant {VariantId}", ct);
                    results.Add((item.VariantId, false, ex.Message, null));
                    failedCount++;
                }
            }

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return ServiceResult<(int, int, int, IEnumerable<(int, bool, string?, int?)>)>.Success((
                itemsList.Count,
                successCount,
                failedCount,
                results
            ));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            await auditService.LogErrorAsync("Bulk stock adjustment failed.", ct);
            return ServiceResult<(int, int, int, IEnumerable<(int, bool, string?, int?)>)>
                .Failure("خطا در تنظیم دسته‌ای موجودی.");
        }
    }

    public async Task<ServiceResult<(int Total, int Success, int Failed, IEnumerable<(int VariantId, bool IsSuccess, string? Error, int? NewStock)> Results)>> BulkStockInAsync(
        IEnumerable<(int VariantId, int Quantity, string? Notes)> items,
        int userId,
        string? supplierReference = null,
        CancellationToken ct = default)
    {
        var itemsList = items.ToList();
        var results = new List<(int, bool, string?, int?)>();
        var successCount = 0; var failedCount = 0;

        await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted, ct);

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
                        var alreadyProcessed = await context.InventoryTransactions
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

                    await inventoryRepository.AddTransactionAsync(tx, ct);
                    results.Add((item.VariantId, true, null, variant.StockQuantity));
                    successCount++;
                }
                catch (Exception ex)
                {
                    await auditService.LogErrorAsync("Error in StockIn for variant {VariantId}", ct);
                    results.Add((item.VariantId, false, ex.Message, null));
                    failedCount++;
                }
            }

            await context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return ServiceResult<(int, int, int, IEnumerable<(int, bool, string?, int?)>)>.Success((
                itemsList.Count,
                successCount,
                failedCount,
                results
            ));
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(ct);
            await auditService.LogErrorAsync("Bulk StockIn failed.", ct);
            return ServiceResult<(int, int, int, IEnumerable<(int, bool, string?, int?)>)>.Unexpected("خطا در Import دسته‌ای موجودی.");
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
            variantId, txType, quantityChange, currentStock, userId, notes, referenceNumber, orderItemId);

        await inventoryRepository.AddTransactionAsync(transaction, ct);
        variant?.AdjustStock(quantityChange);

        if (saveChanges) await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<ServiceResult> ExecuteWithSerializableLockAsync(
        int variantId,
        Func<ProductVariant, Task<ServiceResult>> operation,
        CancellationToken ct = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);

            try
            {
                var variant = await context.Set<ProductVariant>()
                    .FromSqlRaw("SELECT * FROM \"ProductVariants\" WHERE \"Id\" = {0} FOR UPDATE", variantId)
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(ct);

                if (variant is null)
                {
                    await transaction.RollbackAsync(ct);
                    return ServiceResult.Failure("واریانت مورد نظر یافت نشد.");
                }

                var result = await operation(variant);

                if (result.IsSuccess)
                {
                    await context.SaveChangesAsync(ct);
                    await transaction.CommitAsync(ct);
                }
                else
                {
                    await transaction.RollbackAsync(ct);
                }

                return result;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(ct);
                await auditService.LogErrorAsync("Error in serializable lock for variant {VariantId}", ct);
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
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable, ct);

            try
            {
                var variant = await context.Set<ProductVariant>()
                    .FromSqlRaw("SELECT * FROM \"ProductVariants\" WHERE \"Id\" = {0} FOR UPDATE", variantId)
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(ct);

                if (variant is null)
                {
                    await transaction.RollbackAsync(ct);
                    if (typeof(TResult) == typeof(ServiceResult))
                        return (TResult)(object)ServiceResult.NotFound("واریانت مورد نظر یافت نشد.");

                    var failMethod = typeof(TResult).GetMethod("Failure", new[] { typeof(string) });
                    if (failMethod is not null)
                        return (TResult)failMethod.Invoke(null, new object[] { "واریانت مورد نظر یافت نشد." })!;

                    throw new InvalidOperationException("واریانت مورد نظر یافت نشد.");
                }

                var result = await operation(variant);
                await context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(ct);
                await auditService.LogErrorAsync("Error in serializable lock for variant {VariantId}", ct);
                throw;
            }
        });
    }

    private async Task<ProductVariant?> GetVariantWithTrackingAsync(
        int variantId,
        CancellationToken ct = default)
    {
        return await context.Set<ProductVariant>()
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId, ct);
    }

    public async Task ReconcileAsync(
        int variantId,
        int physicalCount,
        string reason,
        int userId,
        int? warehouseId = null,
        CancellationToken ct = default)
    {
        await ExecuteWithSerializableLockAsync(variantId, async variant =>
        {
            var difference = physicalCount - variant.StockQuantity;

            if (difference == 0)
            {
                await auditService.LogInformationAsync(
                    "[InventoryService] Reconcile: Variant={VariantId} - No discrepancy.", ct);
                return ServiceResult.Success();
            }

            await auditService.LogWarningAsync(
                "[InventoryService] Reconcile discrepancy: Variant={VariantId}, System={System}, Physical={Physical}, Delta={Delta}",
                ct);

            var transaction = InventoryTransaction.CreateAdjustment(
                variantId,
                difference,
                variant.StockQuantity,
                userId,
                $"[انبارگردانی] {reason} | سیستم: {variant.StockQuantity}, فیزیکی: {physicalCount}");

            variant.AdjustStock(difference);

            await inventoryRepository.AddTransactionAsync(transaction, ct);

            return ServiceResult.Success();
        }, ct);
    }
}