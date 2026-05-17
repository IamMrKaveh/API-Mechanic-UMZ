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
    INotificationHandler<DomainEventNotification<OrderCreatedEvent>>,
    INotificationHandler<DomainEventNotification<PaymentSucceededEvent>>,
    INotificationHandler<DomainEventNotification<PaymentFailedEvent>>,
    INotificationHandler<DomainEventNotification<OrderCancelledEvent>>,
    INotificationHandler<DomainEventNotification<OrderExpiredEvent>>
{
    public async Task Handle(DomainEventNotification<OrderCreatedEvent> notification, CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;

        var processState = OrderProcessState.Create(
            domainEvent.OrderId,
            correlationId: domainEvent.OrderId.ToString());

        await processStateRepository.AddAsync(processState, ct);
        processState.TransitionTo(ProcessStepEnum.InventoryReserving);
        await unitOfWork.SaveChangesAsync(ct);

        var order = await orderRepository.FindWithItemsByIdAsync(domainEvent.OrderId, ct);
        if (order is null)
        {
            processState.MarkFailed("Order not found after creation.");
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        var referenceNumber = $"ORDER-{order.Id.Value}";

        foreach (var item in order.OrderItems)
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

    public async Task Handle(DomainEventNotification<PaymentSucceededEvent> notification, CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;

        var processState = await processStateRepository.GetByOrderIdAsync(domainEvent.OrderId, ct);
        if (processState is null)
        {
            processState = OrderProcessState.Create(domainEvent.OrderId);
            await processStateRepository.AddAsync(processState, ct);
        }

        var order = await orderRepository.FindWithItemsByIdAsync(domainEvent.OrderId, ct);
        if (order is null)
        {
            processState.MarkFailed("Order not found after payment success.");
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        var paymentTransactionId = Domain.Payment.ValueObjects.PaymentTransactionId.From(domainEvent.PaymentTransactionId.Value);
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

    public async Task Handle(DomainEventNotification<PaymentFailedEvent> notification, CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;

        var processState = await processStateRepository.GetByOrderIdAsync(domainEvent.OrderId, ct);
        if (processState is not null)
        {
            processState.TransitionTo(ProcessStepEnum.PaymentPending);
            processState.IncrementRetry();
            await unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task Handle(DomainEventNotification<OrderCancelledEvent> notification, CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;

        var processState = await processStateRepository.GetByOrderIdAsync(domainEvent.OrderId, ct);
        if (processState is not null)
        {
            processState.MarkCompensating();
            await unitOfWork.SaveChangesAsync(ct);
        }

        await ReleaseInventoryForOrderAsync(domainEvent.OrderId, "لغو سفارش", processState, ct);
    }

    public async Task Handle(DomainEventNotification<OrderExpiredEvent> notification, CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;

        var processState = await processStateRepository.GetByOrderIdAsync(domainEvent.OrderId, ct);
        if (processState is not null)
        {
            processState.MarkCompensating();
            await unitOfWork.SaveChangesAsync(ct);
        }

        await ReleaseInventoryForOrderAsync(domainEvent.OrderId, "انقضای سفارش", processState, ct);
    }

    private async Task CompensateOrderAsync(
    Domain.Order.Aggregates.Order order,
    string failureReason,
    OrderProcessState processState,
    CancellationToken ct)
    {
        await ReleaseInventoryForOrderAsync(order.Id, failureReason, processState, ct);

        processState.MarkFailed(failureReason);

        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task ReleaseInventoryForOrderAsync(
        OrderId orderId,
        string reason,
        OrderProcessState? processState,
        CancellationToken ct)
    {
        var order = await orderRepository.FindWithItemsByIdAsync(orderId, ct);

        if (order is null)
        {
            if (processState is not null)
            {
                processState.MarkFailed("Order not found during inventory release.");
                await unitOfWork.SaveChangesAsync(ct);
            }

            return;
        }

        var referenceNumber = $"ORDER-{order.Id.Value}";

        foreach (var item in order.OrderItems)
        {
            await inventoryService.ReleaseReservationAsync(
                item.VariantId,
                Domain.Inventory.ValueObjects.StockQuantity.Create(item.Quantity),
                referenceNumber,
                reason,
                ct);
        }

        if (processState is not null)
        {
            processState.MarkCompensated();
            await unitOfWork.SaveChangesAsync(ct);
        }
    }
}