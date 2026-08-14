using Application.Cache.Contracts;
using Application.Order.Features.Commands.SetDefaultOrderStatus;
using Domain.Order.Entities;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Order.Features.Commands.SetDefaultOrderStatus;

public class SetDefaultOrderStatusHandlerTests
{
    private readonly IOrderStatusRepository _repository = Substitute.For<IOrderStatusRepository>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly SetDefaultOrderStatusHandler _sut;

    public SetDefaultOrderStatusHandlerTests()
    {
        _sut = new SetDefaultOrderStatusHandler(_repository, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns((OrderStatus?)null);

        var result = await _sut.Handle(new SetDefaultOrderStatusCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenStatusIsInactive_ReturnsValidation()
    {
        var status = OrderStatus.Create("paid", "Paid");
        status.Deactivate();
        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns(status);

        var result = await _sut.Handle(new SetDefaultOrderStatusCommand(status.Id.Value), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Validation);
        _repository.DidNotReceive().Update(Arg.Any<OrderStatus>(), Arg.Any<byte[]?>());
    }

    [Fact]
    public async Task Handle_WhenAlreadyDefault_ReturnsSuccessWithoutSideEffects()
    {
        var status = OrderStatus.Create("paid", "Paid");
        status.SetAsDefault();
        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns(status);

        var result = await _sut.Handle(new SetDefaultOrderStatusCommand(status.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        _repository.DidNotReceive().Update(Arg.Any<OrderStatus>(), Arg.Any<byte[]?>());
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveByPrefixAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenExistingDefaultDiffers_UnsetsPreviousDefaultAndSetsNewOne()
    {
        var newStatus = OrderStatus.Create("shipped", "Shipped");
        var currentDefault = OrderStatus.Create("paid", "Paid");
        currentDefault.SetAsDefault();

        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns(newStatus);
        _repository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns(currentDefault);

        var result = await _sut.Handle(new SetDefaultOrderStatusCommand(newStatus.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        currentDefault.IsDefault.ShouldBeFalse();
        newStatus.IsDefault.ShouldBeTrue();
        _repository.Received(1).Update(currentDefault, Arg.Any<byte[]?>());
        _repository.Received(1).Update(newStatus, Arg.Any<byte[]?>());
        await _cacheService.Received(1).RemoveByPrefixAsync("order-status:", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoExistingDefault_SetsProvidedStatusAsDefault()
    {
        var newStatus = OrderStatus.Create("shipped", "Shipped");
        _repository.GetByIdAsync(Arg.Any<OrderStatusId>(), Arg.Any<CancellationToken>()).Returns(newStatus);
        _repository.GetDefaultAsync(Arg.Any<CancellationToken>()).Returns((OrderStatus?)null);

        var result = await _sut.Handle(new SetDefaultOrderStatusCommand(newStatus.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        newStatus.IsDefault.ShouldBeTrue();
        _repository.Received(1).Update(newStatus, Arg.Any<byte[]?>());
        await _cacheService.Received(1).RemoveByPrefixAsync("order-status:", Arg.Any<CancellationToken>());
    }
}
