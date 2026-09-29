using Application.Audit.Contracts;
using Application.Common.Interfaces;
using Application.Order.Features.Commands.DeleteOrder;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Orders = Domain.Order.Aggregates.Order;

namespace Tests.Application.Order.Features.Commands.DeleteOrder;

public class DeleteOrderHandlerTests : HandlerTestBase
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>(); private readonly DeleteOrderHandler _sut;

    public DeleteOrderHandlerTests()
    {
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _sut = new DeleteOrderHandler(_orderRepository, AuditService, CurrentUserService);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsNotFound()
    {
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns((Orders?)null);

        var result = await _sut.Handle(new DeleteOrderCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenOrderExists_MarksAsDeletedAndUpdates()
    {
        var order = new OrderBuilder().Build();
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new DeleteOrderCommand(order.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        order.IsDeleted.ShouldBeTrue();
        _orderRepository.Received(1).Update(order, Arg.Any<byte[]?>());
    }
}
