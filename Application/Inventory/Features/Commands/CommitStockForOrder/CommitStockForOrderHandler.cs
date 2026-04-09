using Application.Common.Results;
using Domain.Common.Interfaces;
using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Variant.ValueObjects;
using Domain.Order.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Inventory.Features.Commands.CommitStockForOrder;

public class CommitStockForOrderHandler(
    IInventoryRepository inventoryRepository,
    InventoryDomainService inventoryDomainService,
    IUnitOfWork unitOfWork,
    ILogger<CommitStockForOrderHandler> logger) : IRequestHandler<CommitStockForOrderCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(CommitStockForOrderCommand request, CancellationToken ct)
    {
        var errors = new List<string>();

        foreach (var item in request.Items)
        {
            var inventory = await inventoryRepository.GetByVariantIdAsync(VariantId.From(item.VariantId), ct);

            if (inventory is null)
            {
                errors.Add($"موجودی واریانت {item.VariantId} یافت نشد.");
                continue;
            }

            var orderItemId = item.OrderItemId.HasValue ? OrderItemId.From(item.OrderItemId.Value) : null;

            var result = inventoryDomainService.ConfirmReservation(
                inventory,
                item.Quantity,
                request.OrderNumber,
                orderItemId);

            if (result.IsFailure)
            {
                errors.Add($"واریانت {item.VariantId}: {result.Error.Message}");
                continue;
            }

            inventoryRepository.Update(inventory);
        }

        if (errors.Count > 0)
        {
            logger.LogError("Stock commit failed for order {OrderNumber}: {Errors}", request.OrderNumber, string.Join(", ", errors));
            return ServiceResult.Failure(string.Join(" | ", errors));
        }

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Stock committed for order {OrderNumber}", request.OrderNumber);

        return ServiceResult.Success();
    }
}