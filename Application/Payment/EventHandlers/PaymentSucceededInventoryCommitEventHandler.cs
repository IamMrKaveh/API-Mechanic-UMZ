using Domain.Inventory.Interfaces;
using Domain.Inventory.Services;
using Domain.Inventory.ValueObjects;
using Domain.Order.Interfaces;
using Domain.Payment.Events;

namespace Application.Payment.EventHandlers;

public class PaymentSucceededInventoryCommitEventHandler(
    IOrderRepository orderRepository,
    IInventoryRepository inventoryRepository,
    IUnitOfWork unitOfWork) : INotificationHandler<PaymentSucceededEvent>
{
    public async Task Handle(PaymentSucceededEvent notification, CancellationToken ct)
    {
        try
        {
            var order = await orderRepository.FindByIdAsync(notification.OrderId, ct);
            if (order is null) return;

            foreach (var item in order.Items)
            {
                var inventory = await inventoryRepository.GetByVariantIdAsync(item.VariantId, ct);
                if (inventory is null) continue;

                var quantity = StockQuantity.Create(item.Quantity);
                var result = InventoryDomainService.ConfirmReservation(
                    inventory,
                    quantity,
                    order.OrderNumber.Value,
                    item.Id);

                if (result.IsSuccess)
                    inventoryRepository.Update(inventory);
            }

            await unitOfWork.SaveChangesAsync(ct);
        }
        catch
        {
        }
    }
}