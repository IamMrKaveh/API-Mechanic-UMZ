using Application.Notification.Contracts;
using Application.Order.Features.Commands.UpdateOrderStatus;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Orders = Domain.Order.Aggregates.Order;

namespace Tests.Application.Order.Features.Commands.UpdateOrderStatus;

public class UpdateOrderStatusHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>(); private readonly INotificationService _notificationService = Substitute.For<INotificationService>(); private readonly UpdateOrderStatusHandler _sut;

    public UpdateOrderStatusHandlerTests()
    {
        _sut = new UpdateOrderStatusHandler(_orderRepository, _notificationService);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsNotFound()
    {
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns((Orders?)null);

        var result = await _sut.Handle(new UpdateOrderStatusCommand(Guid.NewGuid(), "Paid", "AA=="), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenRowVersionMalformed_ReturnsValidation()
    {
        var order = new OrderBuilder().Build();
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new UpdateOrderStatusCommand(order.Id.Value, "Paid", "@@@"), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
    }

    [Fact]
    public async Task Handle_WhenNewStatusValueIsInvalid_ReturnsFailure()
    {
        var order = new OrderBuilder().Build();
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new UpdateOrderStatusCommand(order.Id.Value, "NotAStatus", "AA=="), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
    }

    [Fact]
    public async Task Handle_WhenTransitionIsNotAllowed_ReturnsValidation()
    {
        var order = new OrderBuilder().Build();
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new UpdateOrderStatusCommand(order.Id.Value, "Delivered", "AA=="), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
    }

    [Fact]
    public async Task Handle_WhenCancelledStatusRequested_ReturnsValidation()
    {
        var order = new OrderBuilder().Build();
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new UpdateOrderStatusCommand(order.Id.Value, "Cancelled", "AA=="), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenValidTransitionToPending_MovesOrderAndNotifies()
    {
        var order = new OrderBuilder().Build();
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new UpdateOrderStatusCommand(order.Id.Value, "Pending", "AA=="), CancellationToken.None);

        result.ShouldBeSuccess();
        order.Status.ShouldBe(OrderStatusValue.Pending);
        _orderRepository.Received(1).Update(order, Arg.Any<byte[]?>());
        await _notificationService.Received(1).SendOrderStatusNotificationAsync(
            order.UserId, order.Id, Arg.Any<string>(), OrderStatusValue.Pending.DisplayName, Arg.Any<CancellationToken>());
    }
}
