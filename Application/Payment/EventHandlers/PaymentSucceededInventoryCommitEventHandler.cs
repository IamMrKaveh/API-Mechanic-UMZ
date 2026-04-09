using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Order.Interfaces;
using Domain.Payment.Events;
using Domain.Variant.ValueObjects;

namespace Application.Payment.EventHandlers;

public class PaymentSucceededInventoryCommitEventHandler(
    IOrderRepository orderRepository,
    IInventoryRepository inventoryRepository,
    InventoryDomainService inventoryDomainService,
    IUnitOfWork unitOfWork,
    ILogger<PaymentSucceededInventoryCommitEventHandler> logger) : INotificationHandler<PaymentSucceededEvent>
{
    public async Task Handle(PaymentSucceededEvent notification, CancellationToken ct)
    {
        try
        {
            var order = await orderRepository.FindByIdAsync(notification.OrderId, ct);
            if (order is null) return;

            foreach (var item in order.Items)
            {
                var inventory = await inventoryRepository.GetByVariantIdAsync(
                    VariantId.From(item.VariantId), ct);

                if (inventory is null) continue;

                var result = inventoryDomainService.ConfirmReservation(
                    inventory, item.Quantity, order.OrderNumber.Value, item.Id);

                if (result.IsSuccess)
                    inventoryRepository.Update(inventory);
                else
                    logger.LogWarning("Failed to commit stock for variant {VariantId}: {Error}",
                        item.VariantId, result.Error);
            }

            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error committing inventory for order {OrderId}", notification.OrderId);
        }
    }
}