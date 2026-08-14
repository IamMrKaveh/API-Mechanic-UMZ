using Application.Common.Interfaces;
using Application.Notification.Contracts;
using Application.Order.Features.Commands.RequestReturn;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Orders = Domain.Order.Aggregates.Order;

namespace Tests.Application.Order.Features.Commands.RequestReturn;

public class RequestReturnHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>(); private readonly INotificationService _notificationService = Substitute.For<INotificationService>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly RequestReturnHandler _sut;

    public RequestReturnHandlerTests()
    {
        _sut = new RequestReturnHandler(_orderRepository, _notificationService, _currentUser);
    }

    private static Orders OrderInDeliveredState(Guid userGuid)
    {
        var order = new OrderBuilder().WithUserId(UserId.From(userGuid)).Build();
        order.MoveToPending();
        order.MarkAsPaid(PaymentTransactionId.From(Guid.NewGuid()));
        order.StartProcessing();
        order.MarkAsShipped();
        order.MarkAsDelivered();
        return order;
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new RequestReturnCommand(Guid.NewGuid(), "reason", null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsNotFound()
    {
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns((Orders?)null);

        var result = await _sut.Handle(new RequestReturnCommand(Guid.NewGuid(), "reason", null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotOwnerAndNotAdmin_ReturnsForbidden()
    {
        var owner = Guid.NewGuid();
        var caller = Guid.NewGuid();
        var order = OrderInDeliveredState(owner);
        _currentUser.UserId.Returns((Guid?)caller);
        _currentUser.IsAdmin.Returns(false);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new RequestReturnCommand(order.Id.Value, "reason", null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
    }

    [Fact]
    public async Task Handle_WhenRowVersionMalformed_ReturnsValidation()
    {
        var owner = Guid.NewGuid();
        var order = OrderInDeliveredState(owner);
        _currentUser.UserId.Returns((Guid?)owner);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new RequestReturnCommand(order.Id.Value, "reason", "@@bad@@"), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
    }

    [Fact]
    public async Task Handle_WhenOrderCannotTransitionToReturned_ReturnsFailure()
    {
        var owner = Guid.NewGuid();
        var order = new OrderBuilder().WithUserId(UserId.From(owner)).Build();
        _currentUser.UserId.Returns((Guid?)owner);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new RequestReturnCommand(order.Id.Value, "reason", null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
        await _notificationService.DidNotReceiveWithAnyArgs().SendOrderStatusNotificationAsync(default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenValidDeliveredOrder_MarksReturnedUpdatesAndNotifies()
    {
        var owner = Guid.NewGuid();
        var order = OrderInDeliveredState(owner);
        _currentUser.UserId.Returns((Guid?)owner);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new RequestReturnCommand(order.Id.Value, "damaged", null), CancellationToken.None);

        result.ShouldBeSuccess();
        order.Status.ShouldBe(OrderStatusValue.Returned);
        _orderRepository.Received(1).Update(order, null);
        await _notificationService.Received(1).SendOrderStatusNotificationAsync(
            order.UserId,
            order.Id,
            Arg.Any<string>(),
            OrderStatusValue.Returned.DisplayName,
            Arg.Any<CancellationToken>());
    }
}
