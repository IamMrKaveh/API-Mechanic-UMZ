using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Variant.ValueObjects;

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
            var inventory = await inventoryRepository.GetByVariantIdAsync(
                VariantId.From(item.VariantId), ct);

            if (inventory is null)
            {
                errors.Add($"موجودی واریانت {item.VariantId} یافت نشد.");
                continue;
            }

            var result = inventoryDomainService.ConfirmReservation(
                inventory, item.Quantity, request.OrderNumber, item.OrderItemId);

            if (!result.IsSuccess)
            {
                errors.Add($"واریانت {item.VariantId}: {result.Error}");
                continue;
            }

            inventoryRepository.Update(inventory);
        }

        if (errors.Count > 0)
        {
            logger.LogError("Stock commit failed for order {OrderNumber}: {Errors}",
                request.OrderNumber, string.Join(", ", errors));
            return ServiceResult.Failure(string.Join(" | ", errors));
        }

        await unitOfWork.SaveChangesAsync(ct);
        logger.LogInformation("Stock committed for order {OrderNumber}", request.OrderNumber);

        return ServiceResult.Success();
    }
}