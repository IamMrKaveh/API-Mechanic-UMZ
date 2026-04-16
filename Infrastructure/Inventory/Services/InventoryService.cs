using Application.Common.Exceptions;
using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Inventory.Services;

public sealed class InventoryService(
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IInventoryService
{
    public async Task<ServiceResult> ReserveStockAsync(
        VariantId variantId,
        StockQuantity quantity,
        string referenceNumber,
        OrderItemId? orderItemId = null,
        CancellationToken ct = default)
    {
        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is null)
            return ServiceResult.NotFound($"موجودی واریانت {variantId.Value} یافت نشد.");

        var result = inventory.ReserveStock(quantity, referenceNumber, orderItemId);
        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Failure("تداخل همزمانی - لطفاً دوباره تلاش کنید.");
        }
    }

    public async Task<ServiceResult> ReleaseReservationAsync(
        VariantId variantId,
        StockQuantity quantity,
        string referenceNumber,
        string? reason = null,
        CancellationToken ct = default)
    {
        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is null)
            return ServiceResult.NotFound($"موجودی واریانت {variantId.Value} یافت نشد.");

        var result = inventory.ReleaseReservation(quantity, referenceNumber, reason);
        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Failure("تداخل همزمانی - لطفاً دوباره تلاش کنید.");
        }
    }

    public async Task<ServiceResult> CommitReservationAsync(
        VariantId variantId,
        StockQuantity quantity,
        string referenceNumber,
        OrderItemId? orderItemId = null,
        CancellationToken ct = default)
    {
        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is null)
            return ServiceResult.NotFound($"موجودی واریانت {variantId.Value} یافت نشد.");

        var result = inventory.ConfirmReservation(quantity, referenceNumber, orderItemId);
        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Failure("تداخل همزمانی - لطفاً دوباره تلاش کنید.");
        }
    }

    public async Task<ServiceResult> IncreaseStockAsync(
        VariantId variantId,
        StockQuantity quantity,
        string reason,
        UserId? userId = null,
        CancellationToken ct = default)
    {
        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is null)
            return ServiceResult.NotFound($"موجودی واریانت {variantId.Value} یافت نشد.");

        var result = inventory.IncreaseStock(quantity, reason, userId);
        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Failure("تداخل همزمانی - لطفاً دوباره تلاش کنید.");
        }
    }

    public async Task<ServiceResult> AdjustStockAsync(
        VariantId variantId,
        StockQuantity quantityChange,
        UserId userId,
        string reason,
        CancellationToken ct = default)
    {
        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is null)
            return ServiceResult.NotFound($"موجودی واریانت {variantId.Value} یافت نشد.");

        var result = inventory.AdjustStock(quantityChange, userId, reason);
        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Failure("تداخل همزمانی - لطفاً دوباره تلاش کنید.");
        }
    }

    public async Task<ServiceResult> ReturnStockAsync(
        VariantId variantId,
        StockQuantity quantity,
        string reason,
        OrderItemId? orderItemId = null,
        UserId? userId = null,
        CancellationToken ct = default)
    {
        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is null)
            return ServiceResult.NotFound($"موجودی واریانت {variantId.Value} یافت نشد.");

        var result = inventory.ReturnStock(quantity, reason, userId);
        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (ConcurrencyException)
        {
            return ServiceResult.Failure("تداخل همزمانی - لطفاً دوباره تلاش کنید.");
        }
    }

    public async Task<ServiceResult> ReturnStockForOrderAsync(
        OrderId orderId,
        Guid adminUserId,
        string reason,
        CancellationToken ct = default)
    {
        var orderItems = await GetOrderItemsAsync(orderId, ct);
        if (!orderItems.Any())
            return ServiceResult.NotFound("آیتمی برای سفارش یافت نشد.");

        var adminUserIdVo = UserId.From(adminUserId);

        foreach (var item in orderItems)
        {
            var inventory = await inventoryRepository.GetByVariantIdAsync(item.VariantId, ct);
            if (inventory is null || inventory.IsUnlimited) continue;

            var quantity = StockQuantity.Create(item.Quantity);
            var result = inventory.ReturnStock(quantity, reason, adminUserIdVo);

            if (result.IsSuccess)
                inventoryRepository.Update(inventory);
            else
                await auditService.LogWarningAsync(
                    $"Return stock failed for variant {item.VariantId.Value}: {result.Error.Message}", ct);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (Exception)
        {
            await auditService.LogErrorAsync($"Failed to return stock for order {orderId.Value}", ct);
            return ServiceResult.Failure("خطا در مرجوعی موجودی سفارش.");
        }
    }

    public async Task<ServiceResult> RollbackReservationsAsync(
        string referenceNumber,
        CancellationToken ct = default)
    {
        var inventories = await GetInventoriesByReferenceAsync(referenceNumber, ct);

        foreach (var inventory in inventories)
        {
            var reservedQty = GetReservedQuantityForReference(inventory, referenceNumber);
            if (reservedQty <= 0) continue;

            var quantity = StockQuantity.Create(reservedQty);
            var result = inventory.ReleaseReservation(quantity, referenceNumber, "آزادسازی خودکار رزرو");

            if (result.IsSuccess)
                inventoryRepository.Update(inventory);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (Exception)
        {
            return ServiceResult.Failure("خطا در آزادسازی رزروها.");
        }
    }

    private async Task<IEnumerable<(VariantId VariantId, int Quantity)>> GetOrderItemsAsync(
        OrderId orderId, CancellationToken ct)
    {
        return await Task.FromResult(Enumerable.Empty<(VariantId, int)>());
    }

    private async Task<IEnumerable<Domain.Inventory.Aggregates.Inventory>> GetInventoriesByReferenceAsync(
        string referenceNumber, CancellationToken ct)
    {
        var entries = await inventoryRepository
            .GetByVariantIdsAsync(Enumerable.Empty<VariantId>(), ct);
        return entries.Where(i => i.LedgerEntries
            .Any(e => e.ReferenceNumber == referenceNumber));
    }

    private static int GetReservedQuantityForReference(Domain.Inventory.Aggregates.Inventory inventory, string referenceNumber)
        => inventory.LedgerEntries
            .Where(e => e.ReferenceNumber == referenceNumber && e.EventType == Domain.Inventory.ValueObjects.StockEventType.Reservation)
            .Sum(e => Math.Abs(e.QuantityDelta));
}