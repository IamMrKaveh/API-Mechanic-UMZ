using Application.Common.Interfaces;
using Application.Inventory.Contracts;
using Application.Order.Features.Commands.ApproveReturn;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Orders = Domain.Order.Aggregates.Order;

namespace Tests.Application.Order.Features.Commands.ApproveReturn;

public class ApproveReturnHandlerTests
{
    private readonly IOrderRepository _orderRepository = Substitute.For<IOrderRepository>(); private readonly IInventoryService _inventoryService = Substitute.For<IInventoryService>(); private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly ApproveReturnHandler _sut;

    public ApproveReturnHandlerTests()
    {
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _sut = new ApproveReturnHandler(_orderRepository, _inventoryService, _unitOfWork, _currentUser);
    }

    private static Orders OrderInDeliveredState()
    {
        var order = new OrderBuilder().Build();
        order.MoveToPending();
        order.MarkAsPaid(PaymentTransactionId.From(Guid.NewGuid()));
        order.StartProcessing();
        order.MarkAsShipped();
        order.MarkAsDelivered();
        return order;
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsNotFound()
    {
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns((Orders?)null);

        var result = await _sut.Handle(new ApproveReturnCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _orderRepository.DidNotReceive().Update(Arg.Any<Orders>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenOrderCannotTransitionToReturned_ReturnsForbidden()
    {
        var order = new OrderBuilder().Build();
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);

        var result = await _sut.Handle(new ApproveReturnCommand(order.Id.Value), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
    }

    [Fact]
    public async Task Handle_WhenInventoryReturnFails_ReturnsFailure()
    {
        var order = OrderInDeliveredState();
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);
        _inventoryService
            .ReturnStockForOrderAsync(Arg.Any<OrderId>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Failure("stock error"));

        var result = await _sut.Handle(new ApproveReturnCommand(order.Id.Value, "reason"), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        _orderRepository.Received(1).Update(order, Arg.Any<byte[]?>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValid_MarksReturnedPersistsAndReturnsStock()
    {
        var order = OrderInDeliveredState();
        _orderRepository.FindByIdAsync(Arg.Any<OrderId>(), Arg.Any<CancellationToken>()).Returns(order);
        _inventoryService
            .ReturnStockForOrderAsync(Arg.Any<OrderId>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        var result = await _sut.Handle(new ApproveReturnCommand(order.Id.Value, "approve"), CancellationToken.None);

        result.ShouldBeSuccess();
        order.Status.ShouldBe(OrderStatusValue.Returned);
        _orderRepository.Received(1).Update(order, Arg.Any<byte[]?>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _inventoryService.Received(1).ReturnStockForOrderAsync(order.Id, Arg.Any<Guid>(), "approve", Arg.Any<CancellationToken>());
    }
}
