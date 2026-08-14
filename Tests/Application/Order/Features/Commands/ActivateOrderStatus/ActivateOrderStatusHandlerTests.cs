using Application.Cache.Contracts;
using Application.Order.Features.Commands.ActivateOrderStatus;
using Domain.Order.Entities;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Order.Features.Commands.ActivateOrderStatus;

public class ActivateOrderStatusHandlerTests
{
    private readonly IOrderStatusRepository _repository = Substitute.For<IOrderStatusRepository>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly ActivateOrderStatusHandler _sut;

    public ActivateOrderStatusHandlerTests()
    {
        _sut = new ActivateOrderStatusHandler(_repository, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns((OrderStatus?)null);

        var result = await _sut.Handle(new ActivateOrderStatusCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _repository.DidNotReceive().Update(Arg.Any<OrderStatus>(), Arg.Any<byte[]?>());
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveByPrefixAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenAlreadyActive_ReturnsSuccessWithoutUpdatingOrInvalidatingCache()
    {
        var status = OrderStatus.Create("paid", "Paid");

        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns(status);

        var result = await _sut.Handle(new ActivateOrderStatusCommand(status.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        _repository.DidNotReceive().Update(Arg.Any<OrderStatus>(), Arg.Any<byte[]?>());
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveByPrefixAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenInactive_ActivatesUpdatesAndInvalidatesCache()
    {
        var status = OrderStatus.Create("paid", "Paid");
        status.Deactivate();

        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns(status);

        var result = await _sut.Handle(new ActivateOrderStatusCommand(status.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        status.IsActive.ShouldBeTrue();
        _repository.Received(1).Update(status, Arg.Any<byte[]?>());
        await _cacheService.Received(1).RemoveByPrefixAsync("order-status:", Arg.Any<CancellationToken>());
    }
}
