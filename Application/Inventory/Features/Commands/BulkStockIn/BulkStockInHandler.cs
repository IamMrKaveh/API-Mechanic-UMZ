using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Variant.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using Domain.User.ValueObjects;

namespace Application.Inventory.Features.Commands.BulkStockIn;

public class BulkStockInHandler(
    IInventoryRepository inventoryRepository,
    InventoryDomainService inventoryDomainService,
    IUnitOfWork unitOfWork,
    ILogger<BulkStockInHandler> logger) : IRequestHandler<BulkStockInCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(BulkStockInCommand request, CancellationToken ct)
    {
        var errors = new List<string>();

        var items = request.VariantIds
            .Zip(request.Quantities, (v, q) => new { VariantId = v, Quantity = q })
            .Zip(request.ReferenceNumbers ?? Enumerable.Repeat((string?)null, request.VariantIds.Count),
                 (vq, r) => new { vq.VariantId, vq.Quantity, Reference = r })
            .ToList();

        foreach (var item in items)
        {
            var inventory = await inventoryRepository.GetByVariantIdAsync(VariantId.From(item.VariantId), ct);

            if (inventory is null)
            {
                errors.Add($"موجودی برای واریانت {item.VariantId} یافت نشد.");
                continue;
            }

            var userId = request.UserId.HasValue ? UserId.From(request.UserId.Value) : null;

            var result = inventory.IncreaseStock(item.Quantity, request.Reason, userId, item.Reference);

            if (result.IsFailure)
            {
                errors.Add($"واریانت {item.VariantId}: {result.Error.Message}");
                continue;
            }

            inventoryRepository.Update(inventory);
        }

        if (errors.Count > 0)
            return ServiceResult.Failure(string.Join(" | ", errors));

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Bulk stock in completed for {Count} variants", request.VariantIds.Count);

        return ServiceResult.Success();
    }
}