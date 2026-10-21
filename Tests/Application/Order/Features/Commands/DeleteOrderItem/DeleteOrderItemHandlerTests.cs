using Application.Common.Interfaces;
using Application.Order.Features.Commands.DeleteOrderItem;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Orders = Domain.Order.Aggregates.Order;

namespace Tests.Application.Order.Features.Commands.DeleteOrderItem;

public class DeleteOrderItemHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly DeleteOrderItemHandler _sut;

    public DeleteOrderItemHandlerTests()
    {
        _sut = new DeleteOrderItemHandler(_orderRepository, _currentUser);
    }

    private void AuthenticateAs(Guid? userId, bool isAdmin = false)
    {
        _currentUser.UserId.Returns(userId);
        _currentUser.IsAuthenticated.Returns(userId is not null);
        _currentUser.IsAdmin.Returns(isAdmin);
    }

    [Fact]
    public async Task Handle_WhenOrderForItemNotFound_ReturnsNotFound()
    {
        _orderRepository.FindByOrderItemIdAsync(Arg.Any<OrderItemId>(), Arg.Any<CancellationToken>()).Returns((Orders?)null);
        AuthenticateAs(Guid.NewGuid());

        var result = await _sut.Handle(new DeleteOrderItemCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenCallerIsNotTheOwnerAndNotAdmin_ReturnsForbidden()
    {
        var order = new OrderBuilder().WithUserId(UserId.NewId()).Build();
        _orderRepository.FindByOrderItemIdAsync(Arg.Any<OrderItemId>(), Arg.Any<CancellationToken>()).Returns(order);
        AuthenticateAs(Guid.NewGuid());

        var result = await _sut.Handle(new DeleteOrderItemCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenItemDoesNotBelongToTheOrder_ReturnsNotFound()
    {
        var order = new OrderBuilder().Build();
        _orderRepository.FindByOrderItemIdAsync(Arg.Any<OrderItemId>(), Arg.Any<CancellationToken>()).Returns(order);
        AuthenticateAs(order.UserId.Value);

        var result = await _sut.Handle(new DeleteOrderItemCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenOwnerRemovesAnExistingItem_RemovesItAndUpdatesTheOrder()
    {
        var snapshot = new OrderItemSnapshotBuilder().Build();
        var order = new OrderBuilder()
            .WithItemSnapshots(snapshot, new OrderItemSnapshotBuilder().Build())
            .Build();
        _orderRepository.FindByOrderItemIdAsync(Arg.Any<OrderItemId>(), Arg.Any<CancellationToken>()).Returns(order);
        AuthenticateAs(order.UserId.Value);

        var itemToRemove = order.OrderItems.First();
        var remainingCount = order.OrderItems.Count - 1;

        var result = await _sut.Handle(new DeleteOrderItemCommand(itemToRemove.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        order.OrderItems.Count.ShouldBe(remainingCount);
        order.OrderItems.ShouldNotContain(i => i.Id == itemToRemove.Id);
        _orderRepository.Received(1).Update(order, Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenRemovingTheLastItem_ReturnsFailure()
    {
        var order = new OrderBuilder().WithItemSnapshots(new OrderItemSnapshotBuilder().Build()).Build();
        _orderRepository.FindByOrderItemIdAsync(Arg.Any<OrderItemId>(), Arg.Any<CancellationToken>()).Returns(order);
        AuthenticateAs(order.UserId.Value);

        var onlyItem = order.OrderItems.Single();

        var result = await _sut.Handle(new DeleteOrderItemCommand(onlyItem.Id.Value), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.BusinessRule);
        order.OrderItems.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Handle_WhenAdminRemovesAnItem_Succeeds()
    {
        var order = new OrderBuilder()
            .WithItemSnapshots(new OrderItemSnapshotBuilder().Build(), new OrderItemSnapshotBuilder().Build())
            .Build();
        _orderRepository.FindByOrderItemIdAsync(Arg.Any<OrderItemId>(), Arg.Any<CancellationToken>()).Returns(order);
        AuthenticateAs(Guid.NewGuid(), isAdmin: true);

        var itemToRemove = order.OrderItems.First();

        var result = await _sut.Handle(new DeleteOrderItemCommand(itemToRemove.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        order.OrderItems.Count.ShouldBe(1);
    }
}
