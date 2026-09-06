using Application.Order.Features.Shared;
using Domain.Order.Aggregates;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Order.Services.Strategies;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Infrastructure.Order.Services.Strategies;

public class CashOnDeliveryCheckoutPaymentStrategyTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly CashOnDeliveryCheckoutPaymentStrategy _sut;

    public CashOnDeliveryCheckoutPaymentStrategyTests()
    {
        _sut = new CashOnDeliveryCheckoutPaymentStrategy(_orderRepository, _unitOfWork);
    }

    private static CheckoutResultDto NewOrderResult(Guid? orderId = null, decimal finalAmount = 150_000m) => new()
    {
        OrderId = orderId ?? Guid.NewGuid(),
        OrderNumber = "ON-1001",
        FinalAmount = finalAmount
    };

    private static global::Domain.Order.Aggregates.Order NewOrder(UserId? userId = null)
    {
        var order = new OrderBuilder().WithUserId(userId ?? UserId.NewId()).Build();
        order.ClearDomainEvents();
        return order;
    }

    [Fact]
    public void Code_IsCashOnDelivery()
    {
        _sut.Code.ShouldBe(PaymentMethodCode.CashOnDelivery);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrderDoesNotExist_ReturnsNotFound()
    {
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns((global::Domain.Order.Aggregates.Order?)null);

        var result = await _sut.ExecuteAsync(
            NewOrderResult(), OrderId.NewId(), UserId.NewId(), Money.FromDecimal(150_000m),
            "127.0.0.1", null, Guid.NewGuid(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrderIsCreated_MovesToPendingAndReturnsUnpaidResult()
    {
        var order = NewOrder();
        order.Status.ShouldBe(OrderStatusValue.Created);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var orderResult = NewOrderResult(order.Id.Value);
        var result = await _sut.ExecuteAsync(
            orderResult, order.Id, UserId.NewId(), Money.FromDecimal(150_000m),
            "127.0.0.1", "agent", Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeSuccess();
        order.Status.ShouldBe(OrderStatusValue.Pending);
        _orderRepository.Received(1).Update(order);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        result.Value.OrderId.ShouldBe(orderResult.OrderId);
        result.Value.PaymentUrl.ShouldBeNull();
        result.Value.PaymentAuthority.ShouldBeNull();
        result.Value.PaymentTransactionId.ShouldBeNull();
        result.Value.IsPaid.ShouldBeFalse();
        result.Value.PaymentMethodCode.ShouldBe(PaymentMethodCode.CashOnDelivery);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrderAlreadyPending_DoesNotTransitionButSaves()
    {
        var order = NewOrder();
        order.MoveToPending();
        order.ClearDomainEvents();
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.ExecuteAsync(
            NewOrderResult(order.Id.Value), order.Id, UserId.NewId(), Money.FromDecimal(150_000m),
            "127.0.0.1", null, Guid.NewGuid(), CancellationToken.None);

        result.ShouldBeSuccess();
        order.Status.ShouldBe(OrderStatusValue.Pending);
        _orderRepository.Received(1).Update(order);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
