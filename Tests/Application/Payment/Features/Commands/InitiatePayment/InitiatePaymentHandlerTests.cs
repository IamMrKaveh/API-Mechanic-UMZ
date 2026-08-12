using Application.Common.Interfaces;
using Application.Payment.Contracts;
using Application.Payment.Features.Commands.InitiatePayment;
using Application.Payment.Features.Shared;
using Domain.Order.Exceptions;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Orders = Domain.Order.Aggregates.Order;

namespace Tests.Application.Payment.Features.Commands.InitiatePayment;

public class InitiatePaymentHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly IPaymentService _paymentService = Substitute.For<IPaymentService>(); private readonly InitiatePaymentHandler _sut;

    public InitiatePaymentHandlerTests()
    {
        _sut = new InitiatePaymentHandler(_orderRepository, _currentUser, _paymentService);
    }

    [Fact]
    public async Task Handle_WhenUserIsAnonymous_ReturnsForbidden()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new InitiatePaymentCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        await _orderRepository.DidNotReceiveWithAnyArgs().FindByIdAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsNotFound()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.IpAddress.Returns((string?)null);
        _orderRepository
            .FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns((Orders?)null);

        var result = await _sut.Handle(new InitiatePaymentCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        await _paymentService.DidNotReceiveWithAnyArgs()
            .InitiatePaymentAsync(default!, default!, default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenOrderBelongsToAnotherUser_ReturnsForbidden()
    {
        var callerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var order = new OrderBuilder().WithUserId(UserId.From(ownerId)).Build();

        _currentUser.UserId.Returns(callerId);
        _currentUser.IpAddress.Returns((string?)null);
        _orderRepository
            .FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _sut.Handle(new InitiatePaymentCommand(order.Id.Value, null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        await _paymentService.DidNotReceiveWithAnyArgs()
            .InitiatePaymentAsync(default!, default!, default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenOrderNotPaidAndCallerIsOwner_ReturnsInitiationResult()
    {
        var callerId = Guid.NewGuid();
        var order = new OrderBuilder().WithUserId(UserId.From(callerId)).Build();

        _currentUser.UserId.Returns(callerId);
        _currentUser.IpAddress.Returns("127.0.0.1");
        _orderRepository
            .FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns(order);

        var expected = new PaymentInitiationResult("A123", "https://gateway/redirect/A123", Guid.NewGuid());
        _paymentService
            .InitiatePaymentAsync(
                Arg.Is<OrderId>(x => x.Value == order.Id.Value),
                order.FinalAmount,
                Arg.Any<IpAddress>(),
                Arg.Is<UserId>(x => x.Value == callerId),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new InitiatePaymentCommand(order.Id.Value, null), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserHasNoIpAddress_UsesUnknownIp()
    {
        var callerId = Guid.NewGuid();
        var order = new OrderBuilder().WithUserId(UserId.From(callerId)).Build();

        _currentUser.UserId.Returns(callerId);
        _currentUser.IpAddress.Returns((string?)null);
        _orderRepository
            .FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns(order);

        _paymentService
            .InitiatePaymentAsync(
                Arg.Any<OrderId>(), Arg.Any<Money>(), Arg.Any<IpAddress>(),
                Arg.Any<UserId>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new PaymentInitiationResult("A", "https://x", Guid.NewGuid()));

        var result = await _sut.Handle(new InitiatePaymentCommand(order.Id.Value, null), CancellationToken.None);

        result.ShouldBeSuccess();
        await _paymentService.Received(1).InitiatePaymentAsync(
            Arg.Any<OrderId>(),
            Arg.Any<Money>(),
            Arg.Is<IpAddress>(x => x.Value == IpAddress.Unknown.Value),
            Arg.Any<UserId>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenPaymentServiceThrowsOrderNotFoundException_ReturnsNotFound()
    {
        var callerId = Guid.NewGuid();
        var order = new OrderBuilder().WithUserId(UserId.From(callerId)).Build();

        _currentUser.UserId.Returns(callerId);
        _currentUser.IpAddress.Returns("127.0.0.1");
        _orderRepository
            .FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns(order);

        _paymentService
            .InitiatePaymentAsync(
                Arg.Any<OrderId>(), Arg.Any<Money>(), Arg.Any<IpAddress>(),
                Arg.Any<UserId>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new OrderNotFoundException(order.Id));

        var result = await _sut.Handle(new InitiatePaymentCommand(order.Id.Value, null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenPaymentServiceThrowsOrderAlreadyPaidException_ReturnsConflict()
    {
        var callerId = Guid.NewGuid();
        var order = new OrderBuilder().WithUserId(UserId.From(callerId)).Build();

        _currentUser.UserId.Returns(callerId);
        _currentUser.IpAddress.Returns("127.0.0.1");
        _orderRepository
            .FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns(order);

        _paymentService
            .InitiatePaymentAsync(
                Arg.Any<OrderId>(), Arg.Any<Money>(), Arg.Any<IpAddress>(),
                Arg.Any<UserId>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new OrderAlreadyPaidException(order.Id));

        var result = await _sut.Handle(new InitiatePaymentCommand(order.Id.Value, null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
    }

    [Fact]
    public async Task Handle_WhenPaymentServiceThrowsExternalServiceException_ReturnsFailure()
    {
        var callerId = Guid.NewGuid();
        var order = new OrderBuilder().WithUserId(UserId.From(callerId)).Build();

        _currentUser.UserId.Returns(callerId);
        _currentUser.IpAddress.Returns("127.0.0.1");
        _orderRepository
            .FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns(order);

        _paymentService
            .InitiatePaymentAsync(
                Arg.Any<OrderId>(), Arg.Any<Money>(), Arg.Any<IpAddress>(),
                Arg.Any<UserId>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new ExternalServiceException("Zarinpal", "gateway timeout"));

        var result = await _sut.Handle(new InitiatePaymentCommand(order.Id.Value, null), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Failure);
    }
}
