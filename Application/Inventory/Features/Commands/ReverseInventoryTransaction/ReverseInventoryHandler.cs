using Domain.Inventory.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Inventory.Features.Commands.ReverseInventoryTransaction;

public class ReverseInventoryHandler(
    IInventoryRepository inventoryRepository,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ReverseInventoryCommand>
{
    public async Task<ServiceResult> Handle(ReverseInventoryCommand request, CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var userId = UserId.From(currentUserService.UserId!.Value);

        var inventoryResult = await (inventoryRepository.GetByVariantIdAsync(variantId, ct)).OrNotFoundAsync("موجودی یافت نشد.");
        if (inventoryResult.IsFailure) return inventoryResult.Error;
        var inventory = inventoryResult.Value;

        var result = inventory.ReverseStockChange(request.IdempotencyKey, request.Reason, userId, dateTimeProvider.UtcNow);

        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);

        await auditService.LogInventoryEventAsync(
            variantId,
            "ReverseInventoryTransaction",
            $"برگشت تراکنش با کلید {request.IdempotencyKey}. دلیل: {request.Reason}",
            userId);

        return ServiceResult.Success();
    }
}
