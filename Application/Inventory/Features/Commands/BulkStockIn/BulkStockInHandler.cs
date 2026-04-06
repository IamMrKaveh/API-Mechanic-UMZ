using Application.Common.Results;
using Domain.Inventory.Interfaces;
using Domain.Variant.ValueObjects;
using Domain.Common.Interfaces;

namespace Application.Inventory.Features.Commands.BulkStockIn;

public class BulkStockInHandler(
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork,
    ILogger<BulkStockInHandler> logger) : IRequestHandler<BulkStockInCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(BulkStockInCommand request, CancellationToken ct)
    {
        var errors = new List<string>();

        foreach (var item in request.Items)
        {
            var inventory = await inventoryRepository.GetByVariantIdAsync(
                ProductVariantId.From(item.VariantId), ct);

            if (inventory is null)
            {
                errors.Add($"موجودی برای واریانت {item.VariantId} یافت نشد.");
                continue;
            }

            try
            {
                inventory.IncreaseStock(item.Quantity, request.Reason, request.UserId, item.ReferenceNumber);
                inventoryRepository.Update(inventory);
            }
            catch (Exception ex)
            {
                errors.Add($"واریانت {item.VariantId}: {ex.Message}");
            }
        }

        if (errors.Count > 0)
            return ServiceResult.Failure(string.Join(" | ", errors));

        await unitOfWork.SaveChangesAsync(ct);
        return ServiceResult.Success();
    }
}