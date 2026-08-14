using Application.Order.Features.Commands.DeleteOrderItem;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Orders = Domain.Order.Aggregates.Order;

namespace Tests.Application.Order.Features.Commands.DeleteOrderItem;

public class DeleteOrderItemHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>(); private readonly DeleteOrderItemHandler _sut;

    public DeleteOrderItemHandlerTests()
    {
        _sut = new DeleteOrderItemHandler(_orderRepository);
    }

    [Fact]
    public async Task Handle_WhenOrderForItemNotFound_ReturnsNotFound()
    {
        _orderRepository.FindByOrderItemIdAsync(Arg.Any<OrderItemId>(), Arg.Any<CancellationToken>()).Returns((Orders?)null);

        var result = await _sut.Handle(new DeleteOrderItemCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenOrderForItemFound_ReturnsSuccessAndInvokesUpdate()
    {
        var order = new OrderBuilder().Build();
        _orderRepository.FindByOrderItemIdAsync(Arg.Any<OrderItemId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new DeleteOrderItemCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldBeSuccess();
        _orderRepository.Received(1).Update(order, Arg.Any<byte[]?>());
    }
}
