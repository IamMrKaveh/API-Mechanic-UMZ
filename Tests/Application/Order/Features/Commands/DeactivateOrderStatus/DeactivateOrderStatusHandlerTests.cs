using Application.Cache.Contracts;
using Application.Order.Features.Commands.DeactivateOrderStatus;
using Domain.Order.Entities;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Order.Features.Commands.DeactivateOrderStatus;

public class DeactivateOrderStatusHandlerTests
{
    private readonly IOrderStatusRepository _repository = Substitute.For<IOrderStatusRepository>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly DeactivateOrderStatusHandler _sut;

    public DeactivateOrderStatusHandlerTests()
    {
        _sut = new DeactivateOrderStatusHandler(_repository, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns((OrderStatus?)null);

        var result = await _sut.Handle(new DeactivateOrderStatusCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenAlreadyInactive_ReturnsSuccessWithoutSideEffects()
    {
        var status = OrderStatus.Create("paid", "Paid");
        status.Deactivate();

        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns(status);

        var result = await _sut.Handle(new DeactivateOrderStatusCommand(status.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        _repository.DidNotReceive().Update(Arg.Any<OrderStatus>(), Arg.Any<byte[]?>());
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveByPrefixAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenActiveAndNonDefault_DeactivatesUpdatesAndInvalidatesCache()
    {
        var status = OrderStatus.Create("paid", "Paid");
        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns(status);

        var result = await _sut.Handle(new DeactivateOrderStatusCommand(status.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        status.IsActive.ShouldBeFalse();
        _repository.Received(1).Update(status, Arg.Any<byte[]?>());
        await _cacheService.Received(1).RemoveByPrefixAsync("order-status:", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStatusIsDefault_ReturnsFailure()
    {
        var status = OrderStatus.Create("paid", "Paid");
        status.SetAsDefault();

        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns(status);

        var result = await _sut.Handle(new DeactivateOrderStatusCommand(status.Id.Value), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Failure);
        _repository.DidNotReceive().Update(Arg.Any<OrderStatus>(), Arg.Any<byte[]?>());
    }
}
