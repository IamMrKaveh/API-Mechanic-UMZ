using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Variant.ValueObjects;
using Domain.User.ValueObjects;
using MediatR;

namespace Application.Inventory.Features.Commands.ReconcileStock;

public class ReconcileStockHandler(
    IInventoryRepository inventoryRepository,
    InventoryDomainService inventoryDomainService,
    IUnitOfWork unitOfWork) : IRequestHandler<ReconcileStockCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ReconcileStockCommand request, CancellationToken ct)
    {
        var inventory = await inventoryRepository.GetByVariantIdAsync(VariantId.From(request.VariantId), ct);

        if (inventory is null)
            return ServiceResult.NotFound("موجودی یافت نشد.");

        var result = inventoryDomainService.Reconcile(
            inventory,
            request.CalculatedStock,
            UserId.From(request.UserId));

        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}