using Application.Common.Results;
using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Variant.ValueObjects;
using Domain.Common.Interfaces;

namespace Application.Inventory.Features.Commands.RecordDamage;

public class RecordDamageHandler(
    IInventoryRepository inventoryRepository,
    InventoryDomainService inventoryDomainService,
    IUnitOfWork unitOfWork) : IRequestHandler<RecordDamageCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(RecordDamageCommand request, CancellationToken ct)
    {
        var inventory = await inventoryRepository.GetByVariantIdAsync(
            ProductVariantId.From(request.VariantId), ct);

        if (inventory is null)
            return ServiceResult.NotFound("موجودی یافت نشد.");

        var result = inventoryDomainService.RecordDamage(
            inventory, request.Quantity, request.UserId, request.Reason);

        if (!result.IsSuccess)
            return ServiceResult.Failure(result.Error!);

        inventoryRepository.Update(inventory);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}