using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Inventory.Interfaces;
using Domain.Variant.ValueObjects;
using Domain.User.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Inventory.Features.Commands.ReverseInventoryTransaction;

public class ReverseInventoryHandler(
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork,
    ILogger<ReverseInventoryHandler> logger) : IRequestHandler<ReverseInventoryCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(ReverseInventoryCommand request, CancellationToken ct)
    {
        var inventory = await inventoryRepository.GetByVariantIdAsync(VariantId.From(request.VariantId), ct);

        if (inventory is null)
            return ServiceResult.NotFound("موجودی یافت نشد.");

        var result = inventory.ReverseStockChange(
            request.IdempotencyKey,
            request.Reason,
            UserId.From(request.UserId));

        if (result.IsFailure)
        {
            logger.LogWarning("Failed to reverse transaction {Key}. Reason: {Reason}", request.IdempotencyKey, result.Error.Message);
            return ServiceResult.Failure(result.Error.Message);
        }

        inventoryRepository.Update(inventory);
        await unitOfWork.SaveChangesAsync(ct);

        return ServiceResult.Success();
    }
}