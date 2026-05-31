using Domain.Inventory.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Commands.BulkAdjustStock;

public class BulkAdjustStockHandler(
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService)
    : IRequestHandler<BulkAdjustStockCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(BulkAdjustStockCommand request, CancellationToken ct)
    {
        if (request.Items.Count == 0)
            return ServiceResult.Failure("لیست اقلام برای تعدیل خالی است.");

        var userId = UserId.From(request.UserId);

        await unitOfWork.ExecuteStrategyAsync(async cancellationToken =>
        {
            foreach (var item in request.Items)
            {
                var variantId = VariantId.From(item.VariantId);

                var inventory = await inventoryRepository.GetByVariantIdAsync(
                    variantId,
                    cancellationToken);

                if (inventory is null)
                    throw new DomainException(
                        $"موجودی برای واریانت {item.VariantId} یافت نشد.");

                var result = inventory.AdjustStock(
                    item.QuantityChange,
                    userId,
                    request.Reason);

                if (result.IsFailure)
                    throw new DomainException(result.Error.Message);

                inventoryRepository.Update(inventory);
            }

            await auditService.LogInventoryEventAsync(
                VariantId.From(request.Items[0].VariantId),
                "BulkAdjustStock",
                $"تعدیل دسته‌ای موجودی برای {request.Items.Count} واریانت. دلیل: {request.Reason}",
                userId);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return 0;
        }, ct);

        return ServiceResult.Success();
    }
}