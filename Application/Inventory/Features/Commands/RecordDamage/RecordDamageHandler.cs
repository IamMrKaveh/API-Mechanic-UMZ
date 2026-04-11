using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Inventory.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Commands.RecordDamage;

public class RecordDamageHandler(
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<RecordDamageCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(RecordDamageCommand request, CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var userId = UserId.From(request.UserId);
        var stock = StockQuantity.Create(request.Quantity);

        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is null)
            return ServiceResult.NotFound("موجودی یافت نشد.");

        var result = InventoryDomainService.RecordDamage(inventory, stock, userId, request.Reason);

        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}