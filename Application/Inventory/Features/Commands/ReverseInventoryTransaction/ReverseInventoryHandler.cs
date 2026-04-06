using Application.Common.Results;
using Domain.Common.Exceptions;
using Domain.Common.Interfaces;
using Domain.Inventory.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Inventory.Features.Commands.ReverseInventoryTransaction;

public class ReverseInventoryHandler(
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork,
    ILogger<ReverseInventoryHandler> logger) : IRequestHandler<ReverseInventoryCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ReverseInventoryCommand request, CancellationToken ct)
    {
        var inventory = await inventoryRepository.GetByVariantIdAsync(
            ProductVariantId.From(request.VariantId), ct);

        if (inventory is null)
            return ServiceResult.NotFound("موجودی یافت نشد.");

        try
        {
            inventory.ReverseStockChange(request.IdempotencyKey, request.Reason, request.UserId);
            inventoryRepository.Update(inventory);
            await unitOfWork.SaveChangesAsync(ct);
            return ServiceResult.Success();
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "Failed to reverse transaction {Key}", request.IdempotencyKey);
            return ServiceResult.Failure(ex.Message);
        }
    }
}