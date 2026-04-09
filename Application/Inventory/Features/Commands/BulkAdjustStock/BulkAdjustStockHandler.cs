using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Variant.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using Domain.User.ValueObjects;

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

        foreach (var variantId in request.VariantId.Zip(request.QuantityChange, (v, q) => new { Variant = v, Quantity = q }))
        {
            var inventory = await inventoryRepository.GetByVariantIdAsync(VariantId.From(variantId.Variant), ct);

            if (inventory is null)
            {
                errors.Add($"موجودی برای واریانت {variantId.Variant} یافت نشد.");
                continue;
            }

            var result = inventoryDomainService.AdjustStock(
                inventory,
                variantId.Quantity,
                UserId.From(request.UserId),
                request.Reason);

            if (result.IsFailure)
            {
                errors.Add($"واریانت {variantId.Variant}: {result.Error.Message}");
                continue;
            }

            inventoryRepository.Update(inventory);
        }

        if (errors.Count > 0)
            return ServiceResult.Failure(string.Join(" | ", errors));

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Bulk stock adjustment completed for {Count} variants", request.VariantId.Count);

        return ServiceResult.Success();
    }
}