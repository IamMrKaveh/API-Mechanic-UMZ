using Application.Common.Results;
using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Variant.ValueObjects;
using Domain.Common.Interfaces;

namespace Application.Inventory.Features.Commands.BulkAdjustStock;

public class BulkAdjustStockHandler(
    IInventoryRepository inventoryRepository,
    InventoryDomainService inventoryDomainService,
    IUnitOfWork unitOfWork,
    ILogger<BulkAdjustStockHandler> logger) : IRequestHandler<BulkAdjustStockCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(BulkAdjustStockCommand request, CancellationToken ct)
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

            var result = inventoryDomainService.AdjustStock(
                inventory, item.QuantityChange, request.UserId, request.Reason);

            if (!result.IsSuccess)
            {
                errors.Add($"واریانت {item.VariantId}: {result.Error}");
                continue;
            }

            inventoryRepository.Update(inventory);
        }

        if (errors.Count > 0)
            return ServiceResult.Failure(string.Join(" | ", errors));

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Bulk stock adjustment completed for {Count} variants", request.Items.Count);

        return ServiceResult.Success();
    }
}