using Application.Audit.Contracts;
using Application.Cache.Contracts;
using Application.Common.Results;
using Application.Inventory.Features.Commands.RemoveStock;
using Domain.Common.Interfaces;
using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;
using MediatR;

namespace Application.Variant.Features.Commands.RemoveStock;

public class RemoveStockHandler(
    IVariantRepository variantRepository,
    IInventoryRepository inventoryRepository,
    InventoryDomainService inventoryDomainService,
    IAuditService auditService,
    IUnitOfWork unitOfWork,
    ICacheService cacheService) : IRequestHandler<RemoveStockCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(RemoveStockCommand request, CancellationToken ct)
    {
        var variant = await variantRepository.GetByIdAsync(VariantId.From(request.VariantId), ct);

        if (variant == null)
            return ServiceResult.NotFound("Variant not found.");

        var inventory = await inventoryRepository.GetByVariantIdAsync(VariantId.From(request.VariantId), ct);

        if (inventory is null)
            return ServiceResult.NotFound("موجودی یافت نشد.");

        var result = inventoryDomainService.DecreaseStock(
            inventory,
            request.Quantity,
            request.Notes ?? "کاهش موجودی",
            UserId.From(request.UserId));

        if (result.IsFailure)
            return ServiceResult.Failure(result.Error.Message);

        inventoryRepository.Update(inventory);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogInventoryEventAsync(
            variant.ProductId.Value,
            "RemoveStock",
            $"Removed {request.Quantity} from stock for variant {request.VariantId}.",
            request.UserId);

        await cacheService.RemoveAsync($"product:{variant.ProductId.Value}", ct);
        await cacheService.RemoveAsync($"variant:{request.VariantId}", ct);

        return ServiceResult.Success();
    }
}