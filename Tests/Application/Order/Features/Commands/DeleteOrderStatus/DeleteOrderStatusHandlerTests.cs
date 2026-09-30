using Application.Order.Features.Commands.DeleteOrderStatus;
using Domain.Order.Entities;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Order.Features.Commands.DeleteOrderStatus;

public class DeleteOrderStatusHandlerTests
{
    private readonly IOrderStatusRepository _repository = Substitute.For<IOrderStatusRepository>(); private readonly DeleteOrderStatusHandler _sut;

    public DeleteOrderStatusHandlerTests()
    {
        _sut = new DeleteOrderStatusHandler(_repository);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns((OrderStatus?)null);

        var result = await _sut.Handle(new DeleteOrderStatusCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _repository.DidNotReceive().Remove(Arg.Any<OrderStatus>());
    }

    [Fact]
    public async Task Handle_WhenStatusIsDefault_ReturnsForbidden()
    {
        var status = OrderStatus.Create("paid", "Paid");
        status.SetAsDefault();
        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns(status);

        var result = await _sut.Handle(new DeleteOrderStatusCommand(status.Id.Value), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        _repository.DidNotReceive().Remove(Arg.Any<OrderStatus>());
    }

    [Fact]
    public async Task Handle_WhenStatusIsInUse_ReturnsForbidden()
    {
        var status = OrderStatus.Create("paid", "Paid");
        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns(status);
        _repository.IsInUseAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns(true);

        var result = await _sut.Handle(new DeleteOrderStatusCommand(status.Id.Value), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        _repository.DidNotReceive().Remove(Arg.Any<OrderStatus>());
    }

    [Fact]
    public async Task Handle_WhenDeletable_Removes()
    {
        var status = OrderStatus.Create("paid", "Paid");
        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns(status);
        _repository.IsInUseAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns(false);

        var result = await _sut.Handle(new DeleteOrderStatusCommand(status.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        _repository.Received(1).Remove(status);
    }
}
