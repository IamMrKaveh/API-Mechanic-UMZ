using Application.Audit.Contracts;
using Domain.Inventory.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Commands.AdjustStock;

public class AdjustStockHandler(
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<AdjustStockCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(AdjustStockCommand request, CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var userId = UserId.From(request.UserId);

        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is null)
            return ServiceResult.NotFound("موجودی یافت نشد.");

        var result = inventory.AdjustStock(request.QuantityChange, userId, request.Reason);

        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogInventoryEventAsync(
            variantId,
            "AdjustStock",
            $"تعدیل موجودی به مقدار {request.QuantityChange} واحد. دلیل: {request.Reason}",
            userId);

        return ServiceResult.Success();
    }
}