using Application.Common.Events;
using Application.Inventory.Contracts;
using Application.Inventory.Features.Commands.CommitStockForOrder;
using Application.Order.Contracts;
using Application.Order.Sagas;
using Application.Order.Sagas.State;
using Application.Payment.Contracts;
using Domain.Inventory.ValueObjects;
using Domain.Order.Enums;
using Domain.Order.Events;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.Events;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;
using Microsoft.FeatureManagement;
using OrderAggregate = Domain.Order.Aggregates.Order;

namespace Tests.Application.Order.Sagas;

public class OrderProcessManagerSagaTests : HandlerTestBase
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IInventoryService _inventoryService = Substitute.For<IInventoryService>();
    private readonly IOrderProcessStateRepository _stateRepository = Substitute.For<IOrderProcessStateRepository>();
    private readonly ISender _mediator = Substitute.For<ISender>();
    private readonly IFeatureManager _featureManager = Substitute.For<IFeatureManager>();
    private readonly IPaymentInitiator _paymentInitiator = Substitute.For<IPaymentInitiator>();
    private readonly OrderProcessManagerSaga _sut;

    public OrderProcessManagerSagaTests()
    {
        _sut = new OrderProcessManagerSaga(
            _orderRepository, _inventoryService, _stateRepository,
            UnitOfWork, _mediator, _featureManager, _paymentInitiator);
        _inventoryService.ReleaseReservationAsync(
                Arg.Any<VariantId>(), Arg.Any<StockQuantity>(), Arg.Any<string>(),
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());
        _mediator.Send(Arg.Any<CommitStockForOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());
    }

    /// <summary>
    /// The production shape: the payment service already marked the order as paid
    /// before the outbox published the event.
    /// </summary>
    private static OrderAggregate PaidOrder()
    {
        var order = new OrderBuilder().Build();
        order.MarkAsPaid(PaymentTransactionId.NewId());
        return order;
    }

    private static DomainEventNotification<PaymentSucceededEvent> Succeeded(OrderAggregate order) =>
        new(new PaymentSucceededEvent(
            PaymentTransactionId.NewId(), order.Id, 1L, order.UserId,
            Money.Create(10m, "IRT"), PaymentGateway.Zarinpal));

    [Fact]
    public async Task Handle_PaymentSucceeded_WhenTheOrderIsAlreadyPaid_StillCommitsTheInventory()
    {
        // The payment service marks the order paid synchronously, so the saga
        // always sees an already paid order. The commit must still happen.
        var order = PaidOrder();
        var state = OrderProcessState.Create(order.Id);
        _stateRepository.GetByOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(state);
        _orderRepository.FindByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await _sut.Handle(Succeeded(order), CancellationToken.None);

        await _mediator.Received(1).Send(
            Arg.Any<CommitStockForOrderCommand>(), Arg.Any<CancellationToken>());
        state.CurrentStep.ShouldBe(ProcessStepEnum.Completed);
    }

    [Fact]
    public async Task Handle_PaymentSucceeded_WhenTheOrderIsAlreadyPaid_MovesTheOrderToProcessing()
    {
        var order = PaidOrder();
        var state = OrderProcessState.Create(order.Id);
        _stateRepository.GetByOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(state);
        _orderRepository.FindByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await _sut.Handle(Succeeded(order), CancellationToken.None);

        order.Status.ShouldBe(OrderStatusValue.Processing);
    }

    [Fact]
    public async Task Handle_PaymentSucceeded_WhenTheOrderIsNotPaidYet_SettlesAndCommitsTheInventory()
    {
        var order = new OrderBuilder().Build();
        var state = OrderProcessState.Create(order.Id);
        _stateRepository.GetByOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(state);
        _orderRepository.FindByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await _sut.Handle(Succeeded(order), CancellationToken.None);

        order.IsPaid.ShouldBeTrue();
        await _mediator.Received(1).Send(
            Arg.Any<CommitStockForOrderCommand>(), Arg.Any<CancellationToken>());
        state.CurrentStep.ShouldBe(ProcessStepEnum.Completed);
    }

    [Fact]
    public async Task Handle_PaymentSucceeded_WhenOrderMissing_MarksFailed()
    {
        var orderId = OrderId.NewId();
        var state = OrderProcessState.Create(orderId);
        _stateRepository.GetByOrderIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(state);
        _orderRepository.FindByIdAsync(orderId, Arg.Any<CancellationToken>())
            .Returns((OrderAggregate?)null);

        await _sut.Handle(new DomainEventNotification<PaymentSucceededEvent>(
            new PaymentSucceededEvent(PaymentTransactionId.NewId(), orderId, 1L, UserId.NewId(), Money.Create(10m, "IRT"), PaymentGateway.Zarinpal)),
            CancellationToken.None);

        state.Status.ShouldBe(ProcessStatusEnum.Failed);
        state.FailureReason.ShouldBe("Order not found after payment success.");
    }

    [Fact]
    public async Task Handle_PaymentSucceeded_WhenNoState_CreatesStateFirst()
    {
        var order = new OrderBuilder().Build();
        _stateRepository.GetByOrderIdAsync(order.Id, Arg.Any<CancellationToken>())
            .Returns((OrderProcessState?)null);
        _orderRepository.FindByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        OrderProcessState? added = null;
        await _stateRepository.AddAsync(Arg.Do<OrderProcessState>(s => added = s), Arg.Any<CancellationToken>());

        await _sut.Handle(Succeeded(order), CancellationToken.None);

        added.ShouldNotBeNull();
        added!.OrderId.ShouldBe(order.Id);
    }

    [Fact]
    public async Task Handle_PaymentSucceeded_WhenCommitFailsAndAutoRefundOn_Refunds()
    {
        var order = PaidOrder();
        var state = OrderProcessState.Create(order.Id);
        _stateRepository.GetByOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(state);
        _orderRepository.FindByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _mediator.Send(Arg.Any<CommitStockForOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure("commit failed"));
        _featureManager.IsEnabledAsync(Arg.Any<string>()).Returns(true);
        _paymentInitiator.InitiateRefundAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        await _sut.Handle(Succeeded(order), CancellationToken.None);

        state.CurrentStep.ShouldBe(ProcessStepEnum.Refunded);
        await _paymentInitiator.Received(1).InitiateRefundAsync(order.Id.Value, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PaymentSucceeded_WhenCommitFailsAndAutoRefundOff_RequiresManualReconciliation()
    {
        var order = PaidOrder();
        var state = OrderProcessState.Create(order.Id);
        _stateRepository.GetByOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(state);
        _orderRepository.FindByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _mediator.Send(Arg.Any<CommitStockForOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure("commit failed"));
        _featureManager.IsEnabledAsync(Arg.Any<string>()).Returns(false);

        await _sut.Handle(Succeeded(order), CancellationToken.None);

        state.CurrentStep.ShouldBe(ProcessStepEnum.RequiresManualReconciliation);
        state.FailureReason.ShouldBe("Auto-refund disabled.");
        await _paymentInitiator.DidNotReceiveWithAnyArgs().InitiateRefundAsync(default, default!, default);
    }

    [Fact]
    public async Task Handle_PaymentFailed_WithState_TransitionsToPendingAndIncrementsRetry()
    {
        var orderId = OrderId.NewId();
        var state = OrderProcessState.Create(orderId);
        _stateRepository.GetByOrderIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(state);

        await _sut.Handle(new DomainEventNotification<PaymentFailedEvent>(
            new PaymentFailedEvent(PaymentTransactionId.NewId(), orderId, "declined")),
            CancellationToken.None);

        state.CurrentStep.ShouldBe(ProcessStepEnum.PaymentPending);
        state.RetryCount.ShouldBe(1);
        await UnitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PaymentFailed_WithoutState_DoesNotThrow()
    {
        var orderId = OrderId.NewId();
        _stateRepository.GetByOrderIdAsync(orderId, Arg.Any<CancellationToken>())
            .Returns((OrderProcessState?)null);

        await Should.NotThrowAsync(() => _sut.Handle(new DomainEventNotification<PaymentFailedEvent>(
            new PaymentFailedEvent(PaymentTransactionId.NewId(), orderId, "declined")),
            CancellationToken.None));

        await UnitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderCancelled_ReleasesInventoryAndMarksCompensated()
    {
        var order = new OrderBuilder().Build();
        var state = OrderProcessState.Create(order.Id);
        _stateRepository.GetByOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(state);
        _orderRepository.FindByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);

        await _sut.Handle(new DomainEventNotification<OrderCancelledEvent>(
            new OrderCancelledEvent(order.Id, order.OrderNumber, order.UserId, "changed mind", false)),
            CancellationToken.None);

        state.CurrentStep.ShouldBe(ProcessStepEnum.Compensated);
        state.Status.ShouldBe(ProcessStatusEnum.Compensated);
        await _inventoryService.Received(order.OrderItems.Count).ReleaseReservationAsync(
            Arg.Any<VariantId>(), Arg.Any<StockQuantity>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderExpired_WithMissingOrder_MarksStateFailed()
    {
        var orderId = OrderId.NewId();
        var state = OrderProcessState.Create(orderId);
        _stateRepository.GetByOrderIdAsync(orderId, Arg.Any<CancellationToken>()).Returns(state);
        _orderRepository.FindByIdAsync(orderId, Arg.Any<CancellationToken>())
            .Returns((OrderAggregate?)null);

        await _sut.Handle(new DomainEventNotification<OrderExpiredEvent>(
            new OrderExpiredEvent(orderId)),
            CancellationToken.None);

        state.Status.ShouldBe(ProcessStatusEnum.Failed);
        state.FailureReason.ShouldBe("Order not found during inventory release.");
    }
}
