using Application.Inventory.Features.Commands.CommitStockForOrder;
using Application.Order.Sagas.State;
using Application.Payment.Features.Adapters;
using Domain.Order.Enums;
using Domain.Order.Events;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.Events;
using Domain.Payment.Services;
using Microsoft.FeatureManagement;
using SharedContracts.FeatureManagement;

namespace Application.Order.Sagas;

public sealed class OrderProcessManagerSaga(
    IOrderRepository orderRepository,
    IInventoryService inventoryService,
    IOrderProcessStateRepository processStateRepository,
    IUnitOfWork unitOfWork,
    ISender mediator,
    IFeatureManager featureManager,
    IPaymentInitiator paymentInitiator) :
    INotificationHandler<DomainEventNotification<PaymentSucceededEvent>>,
    INotificationHandler<DomainEventNotification<PaymentFailedEvent>>,
    INotificationHandler<DomainEventNotification<OrderCancelledEvent>>,
    INotificationHandler<DomainEventNotification<OrderExpiredEvent>>
{
    public async Task Handle(DomainEventNotification<PaymentSucceededEvent> notification, CancellationToken ct)
    {
        var domainEvent = notification.DomainEvent;

        var processState = await processStateRepository.GetByOrderIdAsync(domainEvent.OrderId, ct);
        if (processState is null)
        {
            processState = OrderProcessState.Create(domainEvent.OrderId);
            await processStateRepository.AddAsync(processState, ct);
        }

        var order = await orderRepository.FindByIdAsync(domainEvent.OrderId, ct);
        if (order is null)
        {
            processState.MarkFailed("Order not found after payment success.");
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        var paymentTransactionId = Domain.Payment.ValueObjects.PaymentTransactionId.From(domainEvent.PaymentTransactionId.Value);

        // The payment service already marked the order as paid. Settling it again
        // would be a no-op, but the workflow still has to continue: the order
        // moves on to processing and the reserved stock is committed.
        if (!order.IsPaid)
        {
            var settlementResult = PaymentSettlementService.ProcessPaymentSuccess(
                new OrderPaymentContextAdapter(order), paymentTransactionId);

            if (!settlementResult.IsSuccess)
            {
                processState.MarkFailed($"Settlement failed: {settlementResult.Error}");
                await unitOfWork.SaveChangesAsync(ct);
                return;
            }
        }
        else if (order.Status == OrderStatusValue.Paid)
        {
            order.StartProcessing();
        }

        orderRepository.Update(order);
        processState.TransitionTo(ProcessStepEnum.PaymentSucceeded);
        await unitOfWork.SaveChangesAsync(ct);

        await CommitInventoryOrAutoRefundAsync(order, processState, ct);
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

    private async Task CommitInventoryOrAutoRefundAsync(
        Domain.Order.Aggregates.Order order,
        OrderProcessState processState,
        CancellationToken ct)
    {
        processState.TransitionTo(ProcessStepEnum.InventoryCommitting);
        await unitOfWork.SaveChangesAsync(ct);

        var referenceNumber = $"ORDER-{order.Id.Value}";

        var items = order.OrderItems
            .Select(i => new OrderItemStockCommit(i.VariantId.Value, i.Quantity, i.Id.Value))
            .ToList();

        var commitResult = await mediator.Send(new CommitStockForOrderCommand(items, referenceNumber), ct);

        if (commitResult.IsSuccess)
        {
            processState.MarkCompleted();
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        var autoRefundEnabled = await featureManager.IsEnabledAsync(
            FeatureFlags.SagaAutoRefundOnCommitFailure);

        if (autoRefundEnabled)
        {
            var refundResult = await paymentInitiator.InitiateRefundAsync(
                order.Id.Value,
                $"AutoRefund: inventory commit failed — {commitResult.Error?.Message}",
                ct);

            if (refundResult.IsSuccess)
            {
                processState.MarkRefunded();
            }
            else
            {
                processState.MarkRequiresManualReconciliation(
                    $"Auto-refund failed: {refundResult.Error?.Message}");
            }
        }
        else
        {
            processState.MarkRequiresManualReconciliation("Auto-refund disabled.");
        }

        await unitOfWork.SaveChangesAsync(ct);
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
        var order = await orderRepository.FindByIdAsync(orderId, ct);

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
