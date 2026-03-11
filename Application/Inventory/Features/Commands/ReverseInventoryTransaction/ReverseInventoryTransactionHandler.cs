using Application.Common.Models;
using Domain.Inventory.Entities;
using Domain.Inventory.Interfaces;

namespace Application.Inventory.Features.Commands.ReverseInventoryTransaction;

public class ReverseInventoryTransactionHandler
    : IRequestHandler<ReverseInventoryTransactionCommand, ServiceResult>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReverseInventoryTransactionHandler(
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult> Handle(
        ReverseInventoryTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var transaction = await _inventoryRepository.GetByIdAsync(request.TransactionId, cancellationToken);

        if (transaction is null)
            return ServiceResult.Failure("تراکنش یافت نشد");

        if (transaction.IsReversed)
            return ServiceResult.Failure("این تراکنش قبلاً برگشت خورده است");

        var variant = await _inventoryRepository.GetVariantWithLockAsync(transaction.VariantId, cancellationToken);

        if (variant is null)
            return ServiceResult.Failure("واریانت محصول یافت نشد");

        var reversalQuantity = -transaction.QuantityChange;

        if (!variant.IsUnlimited && variant.StockQuantity + reversalQuantity < 0)
            return ServiceResult.Failure("موجودی نمی‌تواند منفی باشد");

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