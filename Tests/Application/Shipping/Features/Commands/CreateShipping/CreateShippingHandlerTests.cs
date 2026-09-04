using Application.Cache.Contracts;
using Application.Shipping.Features.Commands.CreateShipping;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Mapping;
using DomainShipping = Domain.Shipping.Aggregates.Shipping;

namespace Tests.Application.Shipping.Features.Commands.CreateShipping;

public class CreateShippingHandlerTests : IClassFixture<MapsterConfigFixture>
{
    private readonly IShippingRepository _shippingRepository = Substitute.For<IShippingRepository>(); private readonly ICacheService _cacheService = Substitute.For<ICacheService>(); private readonly CreateShippingHandler _sut;

    public CreateShippingHandlerTests(MapsterConfigFixture fixture)
    {
        _sut = new CreateShippingHandler(_shippingRepository, fixture.Mapper, _cacheService);
    }

    [Fact]
    public async Task Handle_WhenNameAlreadyExists_ReturnsConflictAndDoesNotAdd()
    {
        _shippingRepository
            .ExistsByNameAsync(Arg.Any<ShippingName>(), Arg.Any<ShippingId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new CreateShippingCommand("Express", 50_000m, "desc", null, 1, 3);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        await _shippingRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await _cacheService.DidNotReceiveWithAnyArgs().RemoveByPrefixAsync(default!, default);
    }

    [Fact]
    public async Task Handle_ChecksExistingNameWithNullExcludeId()
    {
        _shippingRepository
            .ExistsByNameAsync(Arg.Any<ShippingName>(), Arg.Any<ShippingId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new CreateShippingCommand("Standard", 25_000m, null, null, 2, 4);

        await _sut.Handle(command, CancellationToken.None);

        await _shippingRepository.Received(1).ExistsByNameAsync(
            Arg.Is<ShippingName>(n => n == ShippingName.Create("Standard")),
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNameIsUnique_AddsShippingAndReturnsMappedDto()
    {
        _shippingRepository
            .ExistsByNameAsync(Arg.Any<ShippingName>(), Arg.Any<ShippingId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        DomainShipping? captured = null;
        await _shippingRepository
            .AddAsync(Arg.Do<DomainShipping>(s => captured = s), Arg.Any<CancellationToken>());

        var command = new CreateShippingCommand("Overnight", 120_000m, "Fastest option", "1-2 days", 1, 2);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe("Overnight");
        result.Value.BaseCost.ShouldBe(120_000m);
        result.Value.Description.ShouldBe("Fastest option");
        result.Value.EstimatedDeliveryTime.ShouldBe("1-2 days");
        result.Value.MinDeliveryDays.ShouldBe(1);
        result.Value.MaxDeliveryDays.ShouldBe(2);
        result.Value.IsActive.ShouldBeTrue();
        result.Value.IsDefault.ShouldBeFalse();

        captured.ShouldNotBeNull();
        captured!.Name.Value.ShouldBe("Overnight");
        captured.BaseCost.Amount.ShouldBe(120_000m);
        captured.DeliveryTime.MinDays.ShouldBe(1);
        captured.DeliveryTime.MaxDays.ShouldBe(2);

        await _shippingRepository.Received(1).AddAsync(Arg.Any<DomainShipping>(), Arg.Any<CancellationToken>());
        await _cacheService.Received(1).RemoveByPrefixAsync("shippings:", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TrimsDescriptionAndEstimatedDeliveryTimeOnCreatedAggregate()
    {
        _shippingRepository
            .ExistsByNameAsync(Arg.Any<ShippingName>(), Arg.Any<ShippingId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        DomainShipping? captured = null;
        await _shippingRepository
            .AddAsync(Arg.Do<DomainShipping>(s => captured = s), Arg.Any<CancellationToken>());

        var command = new CreateShippingCommand("Trimmed", 10_000m, "  padded desc  ", "  padded eta  ", 1, 5);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        captured.ShouldNotBeNull();
        captured!.Description.ShouldBe("padded desc");
        captured.EstimatedDeliveryTime.ShouldBe("padded eta");
    }
}
