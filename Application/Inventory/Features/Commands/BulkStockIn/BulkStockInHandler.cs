using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Inventory.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Commands.BulkStockIn;

public class BulkStockInHandler(
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<BulkStockInCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(BulkStockInCommand request, CancellationToken ct)
    {
        var userId = request.UserId.HasValue ? UserId.From(request.UserId.Value) : null;

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

                var stockQuantity = StockQuantity.Create(item.Quantity);
                var result = InventoryDomainService.IncreaseStock(inventory, stockQuantity, request.Reason, userId);

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

            if (userId is not null && request.Items.Count > 0)
            {
                await auditService.LogInventoryEventAsync(
                    VariantId.From(request.Items[0].VariantId),
                    "BulkStockIn",
                    $"ورود دسته‌ای موجودی برای {request.Items.Count} واریانت. دلیل: {request.Reason}",
                    userId);
            }

            return ServiceResult.Success();
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }
}