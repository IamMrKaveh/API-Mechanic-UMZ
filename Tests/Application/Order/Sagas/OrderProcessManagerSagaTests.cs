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
        _inventoryService.ReserveStockAsync(
                Arg.Any<VariantId>(), Arg.Any<StockQuantity>(), Arg.Any<string>(),
                Arg.Any<OrderItemId?>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());
        _inventoryService.ReleaseReservationAsync(
                Arg.Any<VariantId>(), Arg.Any<StockQuantity>(), Arg.Any<string>(),
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());
    }

    private static DomainEventNotification<OrderCreatedEvent> CreatedNotification(OrderAggregate order) =>
        new(new OrderCreatedEvent(
            order.Id, order.UserId, order.OrderNumber,
            order.FinalAmount.Amount, "IRT", order.OrderItems.Count, Guid.NewGuid()));

    [Fact]
    public async Task Handle_OrderCreated_ReservesInventoryAndMarksReserved()
    {
        var order = new OrderBuilder().Build();
        _orderRepository.FindByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        OrderProcessState? captured = null;
        await _stateRepository.AddAsync(Arg.Do<OrderProcessState>(s => captured = s), Arg.Any<CancellationToken>());

        await _sut.Handle(CreatedNotification(order), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.OrderId.ShouldBe(order.Id);
        captured.CurrentStep.ShouldBe(ProcessStepEnum.InventoryReserved);
        captured.Status.ShouldBe(ProcessStatusEnum.InProgress);
        await _inventoryService.Received(order.OrderItems.Count).ReserveStockAsync(
            Arg.Any<VariantId>(), Arg.Any<StockQuantity>(), $"ORDER-{order.Id.Value}",
            Arg.Any<OrderItemId?>(), Arg.Any<CancellationToken>());
        await UnitOfWork.Received().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_OrderCreated_WhenOrderMissing_MarksFailed()
    {
        var order = new OrderBuilder().Build();
        _orderRepository.FindByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns((OrderAggregate?)null);
        OrderProcessState? captured = null;
        await _stateRepository.AddAsync(Arg.Do<OrderProcessState>(s => captured = s), Arg.Any<CancellationToken>());

        await _sut.Handle(CreatedNotification(order), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Status.ShouldBe(ProcessStatusEnum.Failed);
        captured.FailureReason.ShouldBe("Order not found after creation.");
        await _inventoryService.DidNotReceiveWithAnyArgs().ReserveStockAsync(
            default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task Handle_OrderCreated_WhenReserveFails_CompensatesAndMarksFailed()
    {
        var order = new OrderBuilder().Build();
        _orderRepository.FindByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _inventoryService.ReserveStockAsync(
                Arg.Any<VariantId>(), Arg.Any<StockQuantity>(), Arg.Any<string>(),
                Arg.Any<OrderItemId?>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure("no stock"));
        OrderProcessState? captured = null;
        await _stateRepository.AddAsync(Arg.Do<OrderProcessState>(s => captured = s), Arg.Any<CancellationToken>());

        await _sut.Handle(CreatedNotification(order), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Status.ShouldBe(ProcessStatusEnum.Failed);
        await _inventoryService.Received().ReleaseReservationAsync(
            Arg.Any<VariantId>(), Arg.Any<StockQuantity>(), Arg.Any<string>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
        _orderRepository.Received(1).Update(order, Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_PaymentSucceeded_CommitsInventoryAndCompletes()
    {
        var order = new OrderBuilder().Build();
        var state = OrderProcessState.Create(order.Id);
        _stateRepository.GetByOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(state);
        _orderRepository.FindByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _mediator.Send(Arg.Any<CommitStockForOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        await _sut.Handle(new DomainEventNotification<PaymentSucceededEvent>(
            new PaymentSucceededEvent(PaymentTransactionId.NewId(), order.Id, 123L, order.UserId, Money.Create(100m, "IRT"))),
            CancellationToken.None);

        order.IsPaid.ShouldBeTrue();
        state.CurrentStep.ShouldBe(ProcessStepEnum.Completed);
        state.Status.ShouldBe(ProcessStatusEnum.Completed);
        _orderRepository.Received(1).Update(order, Arg.Any<byte[]?>());
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
            new PaymentSucceededEvent(PaymentTransactionId.NewId(), orderId, 1L, UserId.NewId(), Money.Create(10m, "IRT"))),
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
        _mediator.Send(Arg.Any<CommitStockForOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());
        OrderProcessState? added = null;
        await _stateRepository.AddAsync(Arg.Do<OrderProcessState>(s => added = s), Arg.Any<CancellationToken>());

        await _sut.Handle(new DomainEventNotification<PaymentSucceededEvent>(
            new PaymentSucceededEvent(PaymentTransactionId.NewId(), order.Id, 1L, order.UserId, Money.Create(10m, "IRT"))),
            CancellationToken.None);

        added.ShouldNotBeNull();
        added!.OrderId.ShouldBe(order.Id);
    }

    [Fact]
    public async Task Handle_PaymentSucceeded_WhenCommitFailsAndAutoRefundOn_Refunds()
    {
        var order = new OrderBuilder().Build();
        var state = OrderProcessState.Create(order.Id);
        _stateRepository.GetByOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(state);
        _orderRepository.FindByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _mediator.Send(Arg.Any<CommitStockForOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure("commit failed"));
        _featureManager.IsEnabledAsync(Arg.Any<string>()).Returns(true);
        _paymentInitiator.InitiateRefundAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        await _sut.Handle(new DomainEventNotification<PaymentSucceededEvent>(
            new PaymentSucceededEvent(PaymentTransactionId.NewId(), order.Id, 1L, order.UserId, Money.Create(10m, "IRT"))),
            CancellationToken.None);

        state.CurrentStep.ShouldBe(ProcessStepEnum.Refunded);
        await _paymentInitiator.Received(1).InitiateRefundAsync(order.Id.Value, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PaymentSucceeded_WhenCommitFailsAndAutoRefundOff_RequiresManualReconciliation()
    {
        var order = new OrderBuilder().Build();
        var state = OrderProcessState.Create(order.Id);
        _stateRepository.GetByOrderIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(state);
        _orderRepository.FindByIdAsync(order.Id, Arg.Any<CancellationToken>()).Returns(order);
        _mediator.Send(Arg.Any<CommitStockForOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure("commit failed"));
        _featureManager.IsEnabledAsync(Arg.Any<string>()).Returns(false);

        await _sut.Handle(new DomainEventNotification<PaymentSucceededEvent>(
            new PaymentSucceededEvent(PaymentTransactionId.NewId(), order.Id, 1L, order.UserId, Money.Create(10m, "IRT"))),
            CancellationToken.None);

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
