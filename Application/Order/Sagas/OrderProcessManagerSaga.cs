using Application.Payment.Features.Adapters;
using Domain.Order.Enums;
using Domain.Order.Events;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.Events;
using Domain.Payment.Services;

namespace Application.Order.Sagas;

public sealed class OrderProcessManagerSaga(
    IOrderRepository orderRepository,
    IInventoryService inventoryService,
    IOrderProcessStateRepository processStateRepository,
    IUnitOfWork unitOfWork) :
    INotificationHandler<OrderCreatedEvent>,
    INotificationHandler<PaymentSucceededEvent>,
    INotificationHandler<PaymentFailedEvent>,
    INotificationHandler<OrderCancelledEvent>,
    INotificationHandler<OrderExpiredEvent>
{
    public async Task Handle(OrderCreatedEvent notification, CancellationToken ct)
    {
        var processState = OrderProcessState.Create(
            notification.OrderId,
            correlationId: notification.OrderId.ToString());

        await processStateRepository.AddAsync(processState, ct);
        processState.TransitionTo(ProcessStepEnum.InventoryReserving);
        await unitOfWork.SaveChangesAsync(ct);

        var order = await orderRepository.FindWithItemsByIdAsync(notification.OrderId, ct);
        if (order is null)
        {
            processState.MarkFailed("Order not found after creation.");
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        var referenceNumber = $"ORDER-{order.Id.Value}";

        foreach (var item in order.Items)
        {
            var result = await inventoryService.ReserveStockAsync(
                item.VariantId,
                Domain.Inventory.ValueObjects.StockQuantity.Create(item.Quantity),
                referenceNumber,
                item.Id,
                ct);

            if (!result.IsSuccess)
            {
                processState.MarkCompensating();
                await unitOfWork.SaveChangesAsync(ct);
                await CompensateOrderAsync(order, $"موجودی کافی نیست: {result.Error}", processState, ct);
                return;
            }
        }

        processState.TransitionTo(ProcessStepEnum.InventoryReserved);
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task Handle(PaymentSucceededEvent notification, CancellationToken ct)
    {
        var processState = await processStateRepository.GetByOrderIdAsync(notification.OrderId, ct);
        if (processState is null)
        {
            processState = OrderProcessState.Create(notification.OrderId);
            await processStateRepository.AddAsync(processState, ct);
        }

        var order = await orderRepository.FindWithItemsByIdAsync(notification.OrderId, ct);
        if (order is null)
        {
            processState.MarkFailed("Order not found after payment success.");
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        var paymentTransactionId = Domain.Payment.ValueObjects.PaymentTransactionId.From(notification.PaymentTransactionId.Value);
        var settlementResult = PaymentSettlementService.ProcessPaymentSuccess(
            new OrderPaymentContextAdapter(order), paymentTransactionId);

        if (!settlementResult.IsSuccess)
        {
            processState.MarkFailed($"Settlement failed: {settlementResult.Error}");
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        if (settlementResult.IsIdempotent)
        {
            processState.MarkCompleted();
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        orderRepository.Update(order);
        processState.TransitionTo(ProcessStepEnum.PaymentSucceeded);
        processState.MarkCompleted();
        await unitOfWork.SaveChangesAsync(ct);
    }

    public async Task Handle(PaymentFailedEvent notification, CancellationToken ct)
    {
        var processState = await processStateRepository.GetByOrderIdAsync(notification.OrderId, ct);
        if (processState is not null)
        {
            processState.TransitionTo(ProcessStepEnum.PaymentPending);
            processState.IncrementRetry();
            await unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(OrderCancelledEvent notification, CancellationToken ct)
    {
        var processState = await processStateRepository.GetByOrderIdAsync(notification.OrderId, ct);
        if (processState is not null)
        {
            processState.MarkCompensating();
            await unitOfWork.SaveChangesAsync(ct);
        }

        await ReleaseInventoryForOrderAsync(notification.OrderId, "لغو سفارش", processState, ct);
    }

    public async Task Handle(OrderExpiredEvent notification, CancellationToken ct)
    {
        var processState = await processStateRepository.GetByOrderIdAsync(notification.OrderId, ct);
        if (processState is not null)
        {
            processState.MarkCompensating();
            await unitOfWork.SaveChangesAsync(ct);
        }

        await ReleaseInventoryForOrderAsync(notification.OrderId, "انقضای سفارش", processState, ct);
    }

    private async Task CompensateOrderAsync(
        Domain.Order.Aggregates.Order order,
        string reason,
        OrderProcessState? processState,
        CancellationToken ct)
    {
        try
        {
            var referenceNumber = $"ORDER-{order.Id.Value}";
            await inventoryService.RollbackReservationsAsync(referenceNumber, ct);

            order.Cancel(reason);
            orderRepository.Update(order);

            processState?.MarkCompensated();
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception)
        {
            processState?.MarkFailed("Compensation failed.");
            await unitOfWork.SaveChangesAsync(ct);
        }
    }

    private async Task ReleaseInventoryForOrderAsync(
        OrderId orderId,
        string reason,
        OrderProcessState? processState,
        CancellationToken ct)
    {
        try
        {
            var referenceNumber = $"ORDER-{orderId.Value}";
            var result = await inventoryService.RollbackReservationsAsync(referenceNumber, ct);

            if (result.IsSuccess)
                processState?.MarkCompensated();
            else
                processState?.MarkFailed($"Inventory release failed: {result.Error}");

            if (processState is not null)
                await unitOfWork.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            processState?.MarkFailed($"Exception: {ex.Message}");
            if (processState is not null)
                await unitOfWork.SaveChangesAsync(ct);
        }
    }
}