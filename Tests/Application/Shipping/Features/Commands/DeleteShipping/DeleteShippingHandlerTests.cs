using Application.Cache.Contracts;
using Application.Common.Interfaces;
using Application.Shipping.Features.Commands.DeleteShipping;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using DomainShipping = Domain.Shipping.Aggregates.Shipping;

namespace Tests.Application.Shipping.Features.Commands.DeleteShipping;

public class DeleteShippingHandlerTests
{
    private readonly IShippingRepository _shippingRepository = Substitute.For<IShippingRepository>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly DeleteShippingHandler _sut;

    public DeleteShippingHandlerTests()
    {
        _sut = new DeleteShippingHandler(_shippingRepository, _currentUser, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenShippingNotFound_ReturnsNotFoundAndDoesNotUpdate()
    {
        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns((DomainShipping?)null);

        var result = await _sut.Handle(new DeleteShippingCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _shippingRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveByPrefixAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenShippingExistsAndUserAuthenticated_DeactivatesUpdatesAndReturnsSuccess()
    {
        var shipping = new ShippingBuilder().Build();
        var userGuid = Guid.NewGuid();

        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(shipping);
        _currentUser.UserId.Returns((Guid?)userGuid);

        var result = await _sut.Handle(new DeleteShippingCommand(shipping.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        shipping.IsActive.ShouldBeFalse();
        _shippingRepository.Received(1).Update(shipping);
        await _cacheService.Received(1).RemoveByPrefixAsync("shippings:", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenShippingExistsAndUserNotAuthenticated_UsesNullDeletedByAndReturnsSuccess()
    {
        var shipping = new ShippingBuilder().Build();

        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(shipping);
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new DeleteShippingCommand(shipping.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        shipping.IsActive.ShouldBeFalse();
        _shippingRepository.Received(1).Update(shipping);
    }

    [Fact]
    public async Task Handle_WhenShippingIsDefault_ReturnsFailureAndDoesNotUpdate()
    {
        var shipping = new ShippingBuilder().AsDefault().Build();

        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(shipping);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());

        var result = await _sut.Handle(new DeleteShippingCommand(shipping.Id.Value), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Failure);
        shipping.IsActive.ShouldBeTrue();
        shipping.IsDefault.ShouldBeTrue();
        _shippingRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_LooksUpShippingByIdBuiltFromCommandId()
    {
        var id = Guid.NewGuid();
        ShippingId? captured = null;

        _shippingRepository
            .GetByIdAsync(Arg.Do<ShippingId>(x => captured = x), Arg.Any<CancellationToken>())
            .Returns((DomainShipping?)null);

        _ = await _sut.Handle(new DeleteShippingCommand(id), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(id);
    }
}
