using Domain.Inventory.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Commands.ReverseInventoryTransaction;

public class ReverseInventoryHandler(
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<ReverseInventoryCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ReverseInventoryCommand request, CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var userId = UserId.From(request.UserId);

        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is null)
            return ServiceResult.NotFound("موجودی یافت نشد.");

        var result = inventory.ReverseStockChange(request.IdempotencyKey, request.Reason, userId);

        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogInventoryEventAsync(
            variantId,
            "ReverseInventoryTransaction",
            $"برگشت تراکنش با کلید {request.IdempotencyKey}. دلیل: {request.Reason}",
            userId);

        return ServiceResult.Success();
    }
}