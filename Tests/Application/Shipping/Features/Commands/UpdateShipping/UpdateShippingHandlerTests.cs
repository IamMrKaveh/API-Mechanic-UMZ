using Application.Shipping.Features.Commands.UpdateShipping;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using DomainShipping = Domain.Shipping.Aggregates.Shipping;

namespace Tests.Application.Shipping.Features.Commands.UpdateShipping;

public class UpdateShippingHandlerTests : IClassFixture<IMapper>
{
    private readonly IShippingRepository _shippingRepository = Substitute.For<IShippingRepository>(); private readonly UpdateShippingHandler _sut;

    public UpdateShippingHandlerTests(IMapper _)
    {
        _sut = new UpdateShippingHandler(_shippingRepository, _);
    }

    [Fact]
    public async Task Handle_WhenShippingNotFound_ReturnsNotFoundAndDoesNotCheckNameOrUpdate()
    {
        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns((DomainShipping?)null);

        var command = new UpdateShippingCommand(Guid.NewGuid(), "AnyName", 10_000m, null, null, 1, 3);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        await _shippingRepository.DidNotReceiveWithAnyArgs().ExistsByNameAsync(default!, default, default);
        _shippingRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenAnotherShippingHasSameName_ReturnsConflictAndDoesNotUpdate()
    {
        var shipping = new ShippingBuilder().WithName("OriginalName").Build();

        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(shipping);
        _shippingRepository
            .ExistsByNameAsync(Arg.Any<ShippingName>(), Arg.Any<ShippingId?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var command = new UpdateShippingCommand(shipping.Id.Value, "TakenName", 20_000m, null, null, 1, 3);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Conflict);
        shipping.Name.Value.ShouldBe("OriginalName");
        _shippingRepository.DidNotReceive().Update(shipping);
    }

    [Fact]
    public async Task Handle_ChecksExistingNameExcludingCurrentShippingId()
    {
        var shipping = new ShippingBuilder().Build();

        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(shipping);
        _shippingRepository
            .ExistsByNameAsync(Arg.Any<ShippingName>(), Arg.Any<ShippingId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new UpdateShippingCommand(shipping.Id.Value, "NewName", 30_000m, null, null, 1, 3);

        await _sut.Handle(command, CancellationToken.None);

        await _shippingRepository.Received(1).ExistsByNameAsync(
            Arg.Is<ShippingName>(n => n == ShippingName.Create("NewName")),
            Arg.Is<ShippingId?>(id => id != null && id == shipping.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNameIsUnique_UpdatesAggregatePersistsAndReturnsMappedDto()
    {
        var shipping = new ShippingBuilder()
            .WithName("Before")
            .WithBaseCost(10_000m)
            .WithDeliveryDays(2, 5)
            .Build();

        _shippingRepository
            .GetByIdAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(shipping);
        _shippingRepository
            .ExistsByNameAsync(Arg.Any<ShippingName>(), Arg.Any<ShippingId?>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var command = new UpdateShippingCommand(shipping.Id.Value, "After", 75_000m, "new desc", "1-2 days", 1, 2);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        shipping.Name.Value.ShouldBe("After");
        shipping.BaseCost.Amount.ShouldBe(75_000m);
        shipping.Description.ShouldBe("new desc");
        shipping.EstimatedDeliveryTime.ShouldBe("1-2 days");
        shipping.DeliveryTime.MinDays.ShouldBe(1);
        shipping.DeliveryTime.MaxDays.ShouldBe(2);
        shipping.UpdatedAt.ShouldNotBeNull();

        result.Value.Name.ShouldBe("After");
        result.Value.BaseCost.ShouldBe(75_000m);
        result.Value.MinDeliveryDays.ShouldBe(1);
        result.Value.MaxDeliveryDays.ShouldBe(2);

        _shippingRepository.Received(1).Update(shipping);
    }
}
