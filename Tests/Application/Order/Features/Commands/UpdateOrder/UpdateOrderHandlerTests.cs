using Application.Common.Exceptions;
using Application.Order.Features.Commands.UpdateOrder;
using Application.Order.Features.Shared;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Orders = Domain.Order.Aggregates.Order;
using Shippings = Domain.Shipping.Aggregates.Shipping;

namespace Tests.Application.Order.Features.Commands.UpdateOrder;

public class UpdateOrderHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>(); private readonly IShippingRepository _shippingRepository = Substitute.For<IShippingRepository>(); private readonly UpdateOrderHandler _sut;

    public UpdateOrderHandlerTests()
    {
        _sut = new UpdateOrderHandler(_orderRepository, _shippingRepository);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsNotFound()
    {
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns((Orders?)null);

        var result = await _sut.Handle(new UpdateOrderCommand(Guid.NewGuid(), new UpdateOrderDto(), null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenOrderCannotBeModified_ReturnsForbidden()
    {
        var order = new OrderBuilder().Build();
        order.MoveToPending();
        order.MarkAsPaid(PaymentTransactionId.From(Guid.NewGuid()));

        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new UpdateOrderCommand(order.Id.Value, new UpdateOrderDto(), null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenShippingIdInvalidOrInactive_ReturnsFailure()
    {
        var order = new OrderBuilder().Build();
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);
        _shippingRepository.GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>()).Returns((Shippings?)null);

        var result = await _sut.Handle(
            new UpdateOrderCommand(order.Id.Value, new UpdateOrderDto { ShippingId = Guid.NewGuid() }, null),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenOrderModifiableAndNoShipping_UpdatesAndReturnsSuccess()
    {
        var order = new OrderBuilder().Build();
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new UpdateOrderCommand(order.Id.Value, new UpdateOrderDto(), null), CancellationToken.None);

        result.ShouldBeSuccess();
        _orderRepository.Received(1).Update(order, null);
    }

    [Fact]
    public async Task Handle_WhenUpdateThrowsConcurrencyException_ReturnsConflict()
    {
        var order = new OrderBuilder().Build();
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.When(x => x.Update(Arg.Any<Orders>(), Arg.Any<byte[]?>()))
            .Do(_ => throw new ConcurrencyException());

        var result = await _sut.Handle(new UpdateOrderCommand(order.Id.Value, new UpdateOrderDto(), null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
    }
}
