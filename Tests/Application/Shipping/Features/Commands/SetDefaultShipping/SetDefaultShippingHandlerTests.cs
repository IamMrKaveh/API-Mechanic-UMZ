using Application.Shipping.Features.Commands.SetDefaultShipping;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using DomainShipping = Domain.Shipping.Aggregates.Shipping;

namespace Tests.Application.Shipping.Features.Commands.SetDefaultShipping;

public class SetDefaultShippingHandlerTests
{
    private readonly IShippingRepository _shippingRepository = Substitute.For<IShippingRepository>(); private readonly SetDefaultShippingHandler _sut;

    public SetDefaultShippingHandlerTests()
    {
        _sut = new SetDefaultShippingHandler(_shippingRepository);
    }

    [Fact]
    public async Task Handle_WhenShippingNotFound_ReturnsNotFoundAndDoesNotAccessDefault()
    {
        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns((DomainShipping?)null);

        var result = await _sut.Handle(new SetDefaultShippingCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        await _shippingRepository.DidNotReceiveWithAnyArgs().GetDefaultAsync(default);
        _shippingRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenNoCurrentDefault_SetsShippingAsDefaultAndUpdatesOnce()
    {
        var shipping = new ShippingBuilder().Build();

        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(shipping);
        _shippingRepository
            .GetDefaultAsync(Arg.Any<CancellationToken>())
            .Returns((DomainShipping?)null);

        var result = await _sut.Handle(new SetDefaultShippingCommand(shipping.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        shipping.IsDefault.ShouldBeTrue();
        _shippingRepository.Received(1).Update(shipping);
        _shippingRepository.Received(1).Update(Arg.Any<DomainShipping>());
    }

    [Fact]
    public async Task Handle_WhenADifferentCurrentDefaultExists_UnsetsCurrentDefaultAndPromotesTarget()
    {
        var target = new ShippingBuilder().WithName("Target").Build();
        var currentDefault = new ShippingBuilder().WithName("CurrentDefault").AsDefault().Build();

        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(target);
        _shippingRepository
            .GetDefaultAsync(Arg.Any<CancellationToken>())
            .Returns(currentDefault);

        var result = await _sut.Handle(new SetDefaultShippingCommand(target.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        currentDefault.IsDefault.ShouldBeFalse();
        target.IsDefault.ShouldBeTrue();
        _shippingRepository.Received(1).Update(currentDefault);
        _shippingRepository.Received(1).Update(target);
    }

    [Fact]
    public async Task Handle_WhenTargetIsAlreadyTheCurrentDefault_SkipsUnsetAndUpdatesOnce()
    {
        var shipping = new ShippingBuilder().AsDefault().Build();

        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(shipping);
        _shippingRepository
            .GetDefaultAsync(Arg.Any<CancellationToken>())
            .Returns(shipping);

        var result = await _sut.Handle(new SetDefaultShippingCommand(shipping.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        shipping.IsDefault.ShouldBeTrue();
        _shippingRepository.Received(1).Update(shipping);
    }

    [Fact]
    public async Task Handle_WhenShippingIsInactive_ReturnsFailureAndDoesNotUpdateTarget()
    {
        var shipping = new ShippingBuilder().AsDeleted().Build();
        shipping.IsActive.ShouldBeFalse();

        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(shipping);
        _shippingRepository
            .GetDefaultAsync(Arg.Any<CancellationToken>())
            .Returns((DomainShipping?)null);

        var result = await _sut.Handle(new SetDefaultShippingCommand(shipping.Id.Value), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.Failure);
        shipping.IsDefault.ShouldBeFalse();
        _shippingRepository.DidNotReceive().Update(shipping);
    }
}
