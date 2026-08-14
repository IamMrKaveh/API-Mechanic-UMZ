using Application.Common.Exceptions;
using Application.Common.Interfaces;
using Application.Order.Features.Commands.CancelOrder;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Orders = Domain.Order.Aggregates.Order;

namespace Tests.Application.Order.Features.Commands.CancelOrder;

public class CancelOrderHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly CancelOrderHandler _sut;

    public CancelOrderHandlerTests()
    {
        _sut = new CancelOrderHandler(_orderRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new CancelOrderCommand(Guid.NewGuid(), "reason", null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _orderRepository.DidNotReceiveWithAnyArgs().FindByIdAsync(default!, default);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsNotFound()
    {
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _currentUser.IsAdmin.Returns(true);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns((Orders?)null);

        var result = await _sut.Handle(new CancelOrderCommand(Guid.NewGuid(), "reason", null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenUserIsNotOwnerAndNotAdmin_ReturnsForbidden()
    {
        var ownerGuid = Guid.NewGuid();
        var callerGuid = Guid.NewGuid();
        var order = new OrderBuilder().WithUserId(UserId.From(ownerGuid)).Build();

        _currentUser.UserId.Returns((Guid?)callerGuid);
        _currentUser.IsAdmin.Returns(false);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new CancelOrderCommand(order.Id.Value, "reason", null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenRowVersionIsNotBase64_ReturnsValidation()
    {
        var callerGuid = Guid.NewGuid();
        var order = new OrderBuilder().WithUserId(UserId.From(callerGuid)).Build();

        _currentUser.UserId.Returns((Guid?)callerGuid);
        _currentUser.IsAdmin.Returns(false);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new CancelOrderCommand(order.Id.Value, "reason", "not-base64!!!"), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenOrderCannotBeCancelled_ReturnsFailure()
    {
        var callerGuid = Guid.NewGuid();
        var order = new OrderBuilder().WithUserId(UserId.From(callerGuid)).Build();
        order.MoveToPending();
        order.MarkAsPaid(PaymentTransactionId.From(Guid.NewGuid()));
        order.StartProcessing();
        order.MarkAsShipped();

        _currentUser.UserId.Returns((Guid?)callerGuid);
        _currentUser.IsAdmin.Returns(false);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new CancelOrderCommand(order.Id.Value, "reason", null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenValidOwnerCancels_CancelsAndUpdatesReturningSuccess()
    {
        var callerGuid = Guid.NewGuid();
        var order = new OrderBuilder().WithUserId(UserId.From(callerGuid)).Build();

        _currentUser.UserId.Returns((Guid?)callerGuid);
        _currentUser.IsAdmin.Returns(false);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new CancelOrderCommand(order.Id.Value, "customer changed mind", null), CancellationToken.None);

        result.ShouldBeSuccess();
        order.IsCancelled.ShouldBeTrue();
        order.CancellationReason.ShouldBe("customer changed mind");
        _orderRepository.Received(1).Update(order, null);
    }

    [Fact]
    public async Task Handle_WhenValidRowVersionProvided_PassesDecodedRowVersionToUpdate()
    {
        var callerGuid = Guid.NewGuid();
        var order = new OrderBuilder().WithUserId(UserId.From(callerGuid)).Build();
        var expected = new byte[] { 1, 2, 3, 4 };
        var encoded = Convert.ToBase64String(expected);

        _currentUser.UserId.Returns((Guid?)callerGuid);
        _currentUser.IsAdmin.Returns(false);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new CancelOrderCommand(order.Id.Value, "reason", encoded), CancellationToken.None);

        result.ShouldBeSuccess();
        _orderRepository.Received(1).Update(order, Arg.Is<byte[]?>(rv => rv != null && rv.SequenceEqual(expected)));
    }

    [Fact]
    public async Task Handle_WhenUpdateThrowsConcurrencyException_ReturnsConflict()
    {
        var callerGuid = Guid.NewGuid();
        var order = new OrderBuilder().WithUserId(UserId.From(callerGuid)).Build();

        _currentUser.UserId.Returns((Guid?)callerGuid);
        _currentUser.IsAdmin.Returns(false);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.When(x => x.Update(Arg.Any<Orders>(), Arg.Any<byte[]?>()))
            .Do(_ => throw new ConcurrencyException());

        var result = await _sut.Handle(new CancelOrderCommand(order.Id.Value, "reason", null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
    }

    [Fact]
    public async Task Handle_WhenCallerIsAdminAndNotOwner_CancelsSuccessfully()
    {
        var ownerGuid = Guid.NewGuid();
        var adminGuid = Guid.NewGuid();
        var order = new OrderBuilder().WithUserId(UserId.From(ownerGuid)).Build();

        _currentUser.UserId.Returns((Guid?)adminGuid);
        _currentUser.IsAdmin.Returns(true);
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new CancelOrderCommand(order.Id.Value, "admin cancel", null), CancellationToken.None);

        result.ShouldBeSuccess();
        order.IsCancelled.ShouldBeTrue();
        _orderRepository.Received(1).Update(order, null);
    }
}
