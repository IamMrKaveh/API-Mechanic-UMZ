using Application.Common.Exceptions;
using Application.Order.Features.Commands.MarkOrderAsShipped;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Orders = Domain.Order.Aggregates.Order;

namespace Tests.Application.Order.Features.Commands.MarkOrderAsShipped;

public class MarkOrderAsShippedHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>(); private readonly MarkOrderAsShippedHandler _sut;

    public MarkOrderAsShippedHandlerTests()
    {
        _sut = new MarkOrderAsShippedHandler(_orderRepository);
    }

    private static Orders OrderInProcessingState()
    {
        var order = new OrderBuilder().Build();
        order.MoveToPending();
        order.MarkAsPaid(PaymentTransactionId.From(Guid.NewGuid()));
        order.StartProcessing();
        return order;
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsNotFound()
    {
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns((Orders?)null);

        var result = await _sut.Handle(new MarkOrderAsShippedCommand(Guid.NewGuid(), null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenRowVersionMalformed_ReturnsValidation()
    {
        var order = OrderInProcessingState();
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new MarkOrderAsShippedCommand(order.Id.Value, "###not-base64###"), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenOrderNotInProcessing_ReturnsFailure()
    {
        var order = new OrderBuilder().Build();
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new MarkOrderAsShippedCommand(order.Id.Value, null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenOrderInProcessing_MarksShippedAndUpdatesReturningSuccess()
    {
        var order = OrderInProcessingState();
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new MarkOrderAsShippedCommand(order.Id.Value, null), CancellationToken.None);

        result.ShouldBeSuccess();
        order.IsShipped.ShouldBeTrue();
        _orderRepository.Received(1).Update(order, null);
    }

    [Fact]
    public async Task Handle_WhenUpdateThrowsConcurrencyException_ReturnsConflict()
    {
        var order = OrderInProcessingState();
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);
        _orderRepository.When(x => x.Update(Arg.Any<Orders>(), Arg.Any<byte[]?>()))
            .Do(_ => throw new ConcurrencyException());

        var result = await _sut.Handle(new MarkOrderAsShippedCommand(order.Id.Value, null), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
    }
}
