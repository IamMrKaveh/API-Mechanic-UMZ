using Application.Audit.Contracts;
using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Commands.BulkAdjustStock;

public class BulkAdjustStockHandler(
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<BulkAdjustStockCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(BulkAdjustStockCommand request, CancellationToken ct)
    {
        var userId = UserId.From(request.UserId);

        using var transaction = await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var errors = new List<string>();

            foreach (var item in request.Items)
            {
                var variantId = VariantId.From(item.VariantId);
                var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);

                if (inventory is null)
                {
                    errors.Add($"موجودی برای واریانت {item.VariantId} یافت نشد.");
                    continue;
                }

                var result = inventory.AdjustStock(item.QuantityChange, userId, request.Reason);

                if (result.IsFailure)
                {
                    errors.Add($"واریانت {item.VariantId}: {result.Error.Message}");
                    continue;
                }

                inventoryRepository.Update(inventory);
            }

            if (errors.Count > 0)
            {
                await unitOfWork.RollbackTransactionAsync(ct);
                return ServiceResult.Failure(string.Join(" | ", errors));
            }

            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);

            await auditService.LogInventoryEventAsync(
                VariantId.From(request.Items[0].VariantId),
                "BulkAdjustStock",
                $"تعدیل دسته‌ای موجودی برای {request.Items.Count} واریانت. دلیل: {request.Reason}",
                userId);

            return ServiceResult.Success();
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }
}