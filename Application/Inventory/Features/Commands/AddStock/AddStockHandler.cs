using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Commands.AddStock;

public class AddStockHandler(
    IInventoryRepository inventoryRepository,
    InventoryDomainService inventoryDomainService,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<AddStockCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(AddStockCommand request, CancellationToken ct)
    {
        var inventory = await inventoryRepository.GetByVariantIdAsync(VariantId.From(request.VariantId), ct);

        if (inventory is null)
            return ServiceResult.NotFound("موجودی یافت نشد.");

        var result = inventoryDomainService.IncreaseStock(
            inventory,
            request.Quantity,
            request.Notes ?? "افزایش موجودی",
            UserId.From(request.UserId));

        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogInventoryEventAsync(
            request.VariantId,
            "AddStock",
            $"Added {request.Quantity} units via AddStockCommand.",
            request.UserId);

        return ServiceResult.Success();
    }
}