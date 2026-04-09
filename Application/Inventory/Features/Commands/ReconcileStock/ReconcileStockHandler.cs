using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Commands.ReconcileStock;

public class ReconcileStockHandler(
    IInventoryRepository inventoryRepository,
    InventoryDomainService inventoryDomainService,
    IUnitOfWork unitOfWork) : IRequestHandler<ReconcileStockCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ReconcileStockCommand request, CancellationToken ct)
    {
        var inventory = await inventoryRepository.GetByVariantIdAsync(
            VariantId.From(request.VariantId), ct);

        if (inventory is null)
            return ServiceResult.NotFound("موجودی یافت نشد.");

        var result = inventoryDomainService.Reconcile(inventory, request.CalculatedStock, request.UserId);

        inventoryRepository.Update(inventory);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}