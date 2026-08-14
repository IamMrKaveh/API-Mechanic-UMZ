using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Order.Features.Commands.ConfirmDelivery;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Orders = Domain.Order.Aggregates.Order;

namespace Tests.Application.Order.Features.Commands.ConfirmDelivery;

public class ConfirmDeliveryHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly ConfirmDeliveryHandler _sut;

    public ConfirmDeliveryHandlerTests()
    {
        _sut = new ConfirmDeliveryHandler(_orderRepository, _currentUser);
    }

    private static Orders OrderInShippedState(Guid userGuid)
    {
        var order = new OrderBuilder().WithUserId(UserId.From(userGuid)).Build();
        order.MoveToPending();
        order.MarkAsPaid(PaymentTransactionId.From(Guid.NewGuid()));
        order.StartProcessing();
        order.MarkAsShipped();
        return order;
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new ConfirmDeliveryCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _orderRepository.DidNotReceiveWithAnyArgs().FindByIdAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsNotFound()
    {
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns((Orders?)null);

        var result = await _sut.Handle(new ConfirmDeliveryCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotOwnerAndNotAdmin_ReturnsForbidden()
    {
        var ownerGuid = Guid.NewGuid();
        var callerGuid = Guid.NewGuid();
        var order = OrderInShippedState(ownerGuid);

        _currentUser.UserId.Returns((Guid?)callerGuid);
        _currentUser.IsAdmin.Returns(false);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new ConfirmDeliveryCommand(order.Id.Value, null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
    }

    [Fact]
    public async Task Handle_WhenRowVersionInvalidBase64_ReturnsValidation()
    {
        var callerGuid = Guid.NewGuid();
        var order = OrderInShippedState(callerGuid);

        _currentUser.UserId.Returns((Guid?)callerGuid);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new ConfirmDeliveryCommand(order.Id.Value, "!!!"), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
    }

    [Fact]
    public async Task Handle_WhenOrderNotShipped_ReturnsFailureAndDoesNotUpdate()
    {
        var callerGuid = Guid.NewGuid();
        var order = new OrderBuilder().WithUserId(UserId.From(callerGuid)).Build();

        _currentUser.UserId.Returns((Guid?)callerGuid);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new ConfirmDeliveryCommand(order.Id.Value, null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenOwnerConfirmsShippedOrder_MarksDeliveredAndUpdates()
    {
        var callerGuid = Guid.NewGuid();
        var order = OrderInShippedState(callerGuid);

        _currentUser.UserId.Returns((Guid?)callerGuid);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new ConfirmDeliveryCommand(order.Id.Value, null), CancellationToken.None);

        result.ShouldBeSuccess();
        order.IsDelivered.ShouldBeTrue();
        order.DeliveredAt.ShouldNotBeNull();
        _orderRepository.Received(1).Update(order, null);
    }

    [Fact]
    public async Task Handle_WhenUpdateThrowsConcurrencyException_ReturnsConflict()
    {
        var callerGuid = Guid.NewGuid();
        var order = OrderInShippedState(callerGuid);

        _currentUser.UserId.Returns((Guid?)callerGuid);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.When(x => x.Update(Arg.Any<Orders>(), Arg.Any<byte[]?>()))
            .Do(_ => throw new ConcurrencyException());

        var result = await _sut.Handle(new ConfirmDeliveryCommand(order.Id.Value, null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
    }
}
