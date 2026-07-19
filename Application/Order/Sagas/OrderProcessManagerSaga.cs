using Application.Inventory.Features.Commands.CommitStockForOrder;
using Application.Order.Sagas.State;
using Application.Payment.Features.Adapters;
using Application.Payment.Features.Commands.AtomicRefundPayment;
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
    IUnitOfWork unitOfWork,
    ISender mediator,
    IFeatureManager featureManager,
    IAuditService auditService) :
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

        var order = await orderRepository.FindByIdAsync(domainEvent.OrderId, ct);
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

        var order = await orderRepository.FindByIdAsync(domainEvent.OrderId, ct);
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

        var items = order.OrderItems
            .Select(i => new OrderItemStockCommit(i.VariantId.Value, i.Quantity, i.Id.Value))
            .ToList();

        var orderNumber = order.OrderNumber?.Value ?? $"ORDER-{order.Id.Value}";

        ServiceResult commitResult;
        try
        {
            commitResult = await mediator.Send(
                new CommitStockForOrderCommand(items, orderNumber),
                ct);
        }
        catch (Exception ex)
        {
            commitResult = ServiceResult.Failure(ex.Message);
        }

        if (commitResult.IsSuccess)
        {
            processState.MarkCompleted();
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        var failureReason = commitResult.Error?.Message ?? "Inventory commit failed after payment.";

        processState.MarkInventoryCommitFailed(failureReason);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogErrorAsync(
            $"InventoryCommitFailed for Order {order.Id.Value}: {failureReason}",
            ct);

        var autoRefundEnabled = await featureManager.IsEnabledAsync(
            FeatureManagementExtensions.Flags.SagaAutoRefundOnCommitFailure);

        if (!autoRefundEnabled)
        {
            processState.MarkRequiresManualReconciliation(
                $"Auto-refund disabled. Original failure: {failureReason}");
            await unitOfWork.SaveChangesAsync(ct);
            return;
        }

        ServiceResult refundResult;
        try
        {
            refundResult = await mediator.Send(
                new AtomicRefundPaymentCommand(order.Id.Value, $"جبران خودکار به دلیل عدم موفقیت در ثبت موجودی: {failureReason}"),
                ct);
        }
        catch (Exception ex)
        {
            refundResult = ServiceResult.Failure(ex.Message);
        }

        if (refundResult.IsSuccess)
        {
            processState.MarkRefunded();
            await unitOfWork.SaveChangesAsync(ct);

            await ReleaseInventoryForOrderAsync(
                order.Id,
                "جبران خودکار پس از شکست ثبت موجودی",
                null,
                ct);
            return;
        }

        var refundError = refundResult.Error?.Message ?? "Automatic refund failed.";

        processState.MarkRequiresManualReconciliation(
            $"Commit failure: {failureReason}. Refund failure: {refundError}");
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogErrorAsync(
            $"RequiresManualReconciliation for Order {order.Id.Value}: {refundError}",
            ct);
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
