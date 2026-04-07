using Application.Common.Results;
using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Variant.ValueObjects;
using Domain.Common.Interfaces;

namespace Application.Inventory.Features.Commands.AdjustStock;

public class AdjustStockHandler(
    IInventoryRepository inventoryRepository,
    InventoryDomainService inventoryDomainService,
    IUnitOfWork unitOfWork,
    ILogger<AdjustStockHandler> logger) : IRequestHandler<AdjustStockCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(AdjustStockCommand request, CancellationToken ct)
    {
        var inventory = await inventoryRepository.GetByVariantIdAsync(
            VariantId.From(request.VariantId), ct);

        if (inventory is null)
            return ServiceResult.NotFound("موجودی یافت نشد.");

        var result = inventoryDomainService.AdjustStock(
            inventory, request.QuantityChange, request.UserId, request.Reason);

        if (!result.IsSuccess)
            return ServiceResult.Failure(result.Error!);

        inventoryRepository.Update(inventory);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation(
            "Stock adjusted for variant {VariantId} by {Change}",
            request.VariantId, request.QuantityChange);

        return ServiceResult.Success();
    }
}