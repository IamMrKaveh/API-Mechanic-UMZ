using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Inventory.Interfaces;

namespace Application.Inventory.Features.Commands.ReverseInventoryTransaction;

public class ReverseInventoryTransactionHandler(
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork)
        : IRequestHandler<ReverseInventoryTransactionCommand, ServiceResult>
{
    private readonly IInventoryRepository _inventoryRepository = inventoryRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<ServiceResult> Handle(
        ReverseInventoryTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var transaction = await _inventoryRepository.GetByIdAsync(request.TransactionId, cancellationToken);

        if (transaction is null)
            return ServiceResult.NotFound("تراکنش یافت نشد");

        if (transaction.IsReversed)
            return ServiceResult.Conflict("این تراکنش قبلاً برگشت خورده است");

        var variant = await _inventoryRepository.GetVariantWithLockAsync(transaction.VariantId, cancellationToken);

        if (variant is null)
            return ServiceResult.NotFound("واریانت محصول یافت نشد");

        var reversalQuantity = -transaction.QuantityChange;

        if (!variant.IsUnlimited && variant.StockQuantity + reversalQuantity < 0)
            return ServiceResult.Forbidden("موجودی نمی‌تواند منفی باشد");

        transaction.MarkAsReversed();

        var reversalTransaction = InventoryTransaction.Create(
            variantId: transaction.VariantId,
            transactionType: TransactionType.Adjustment,
            quantityChange: reversalQuantity,
            stockBefore: variant.StockQuantity,
            userId: request.AdminUserId,
            notes: $"برگشت تراکنش #{transaction.Id} - {request.Reason}",
            referenceNumber: $"REV-{transaction.Id}");

        if (!variant.IsUnlimited)
            variant.SetStock(variant.StockQuantity + reversalQuantity);

        await _inventoryRepository.AddTransactionAsync(reversalTransaction, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult.Success();
    }
}