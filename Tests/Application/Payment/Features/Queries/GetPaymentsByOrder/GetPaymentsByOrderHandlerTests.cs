using Application.Common.Interfaces;
using Application.Payment.Contracts;
using Application.Payment.Features.Queries.GetPaymentsByOrder;
using Application.Payment.Features.Shared;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Orders = Domain.Order.Aggregates.Order;

namespace Tests.Application.Payment.Features.Queries.GetPaymentsByOrder;

public class GetPaymentsByOrderHandlerTests
{
    private readonly IPaymentQueryService _paymentQueryService = Substitute.For<IPaymentQueryService>(); private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly GetPaymentsByOrderHandler _sut;

    public GetPaymentsByOrderHandlerTests()
    {
        _sut = new GetPaymentsByOrderHandler(_paymentQueryService, _orderRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenUserIsAnonymous_ReturnsUnauthorized()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new GetPaymentsByOrderQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _orderRepository.DidNotReceiveWithAnyArgs().FindByIdAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsNotFound()
    {
        _currentUser.UserId.Returns(Guid.NewGuid());
        _orderRepository
            .FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns((Orders?)null);

        var result = await _sut.Handle(new GetPaymentsByOrderQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenNonAdminAccessesOtherUsersOrder_ReturnsForbidden()
    {
        var callerId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var order = new OrderBuilder().WithUserId(UserId.From(ownerId)).Build();

        _currentUser.UserId.Returns(callerId);
        _currentUser.IsAdmin.Returns(false);
        _orderRepository
            .FindByIdAsync(Arg.Is<OrderId>(x => x == order.Id), Arg.Any<CancellationToken>())
            .Returns(order);

        var result = await _sut.Handle(new GetPaymentsByOrderQuery(order.Id.Value), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        await _paymentQueryService.DidNotReceiveWithAnyArgs().GetByOrderIdAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenOwnerRequestsOwnOrder_ReturnsSuccess()
    {
        var callerId = Guid.NewGuid();
        var order = new OrderBuilder().WithUserId(UserId.From(callerId)).Build();

        _currentUser.UserId.Returns(callerId);
        _currentUser.IsAdmin.Returns(false);
        _orderRepository
            .FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns(order);

        var dtos = new List<PaymentTransactionDto>
    {
        new() { Id = Guid.NewGuid(), OrderId = order.Id.Value, UserId = callerId }
    };
        _paymentQueryService
            .GetByOrderIdAsync(Arg.Is<OrderId>(x => x == order.Id), Arg.Any<CancellationToken>())
            .Returns(dtos);

        var result = await _sut.Handle(new GetPaymentsByOrderQuery(order.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(dtos);
    }

    [Fact]
    public async Task Handle_WhenAdminRequestsAnyOrder_ReturnsSuccess()
    {
        var ownerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var order = new OrderBuilder().WithUserId(UserId.From(ownerId)).Build();

        _currentUser.UserId.Returns(adminId);
        _currentUser.IsAdmin.Returns(true);
        _orderRepository
            .FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns(order);

        var dtos = new List<PaymentTransactionDto>
    {
        new() { Id = Guid.NewGuid(), OrderId = order.Id.Value, UserId = ownerId }
    };
        _paymentQueryService
            .GetByOrderIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>())
            .Returns(dtos);

        var result = await _sut.Handle(new GetPaymentsByOrderQuery(order.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(dtos);
    }
}
