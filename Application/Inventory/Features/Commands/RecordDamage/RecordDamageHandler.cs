using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Inventory.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using SharedKernel.Abstractions.Interfaces;

namespace Application.Inventory.Features.Commands.RecordDamage;

public class RecordDamageHandler(
    IInventoryRepository inventoryRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RecordDamageCommand>
{
    public async Task<ServiceResult> Handle(RecordDamageCommand request, CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var userId = UserId.From(currentUserService.UserId!.Value);
        var stock = StockQuantity.Create(request.Quantity);

        var inventoryResult = await (inventoryRepository.GetByVariantIdAsync(variantId, ct)).OrNotFoundAsync("موجودی یافت نشد.");
        if (inventoryResult.IsFailure) return inventoryResult.Error;
        var inventory = inventoryResult.Value;

        var result = InventoryDomainService.RecordDamage(inventory, stock, userId, request.Reason, dateTimeProvider.UtcNow);

        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);

        return ServiceResult.Success();
    }
}
