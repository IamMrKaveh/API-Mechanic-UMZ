using Application.Shipping.Contracts;
using Application.Shipping.Features.Queries.CalculateShippingCost;
using Application.Shipping.Features.Shared;
using Domain.Shipping.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Application.Shipping.Features.Queries.CalculateShippingCost;

public class CalculateShippingCostHandlerTests
{
    private readonly IShippingQueryService _shippingQueryService = Substitute.For<IShippingQueryService>();
    private readonly CalculateShippingCostHandler _sut;

    public CalculateShippingCostHandlerTests()
    {
        _sut = new CalculateShippingCostHandler(_shippingQueryService);
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsResult_ReturnsSuccessWithDto()
    {
        var shippingId = Guid.NewGuid();
        var expected = new ShippingCostResultDto
        {
            ShippingId = shippingId,
            ShippingName = "Express",
            Cost = 80_000m,
            IsFree = false,
            MinDeliveryDays = 1,
            MaxDeliveryDays = 3
        };

        _shippingQueryService
            .CalculateShippingCostAsync(
                Arg.Any<ShippingId>(),
                Arg.Any<Money>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(
            new CalculateShippingCostQuery(shippingId, 500_000m),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
        result.Value.Cost.ShouldBe(80_000m);
        result.Value.IsFree.ShouldBeFalse();
    }

    [Fact]
    public async Task Handle_WhenOrderQualifiesForFreeShipping_ReturnsSuccessWithFreeDto()
    {
        var shippingId = Guid.NewGuid();
        var expected = new ShippingCostResultDto
        {
            ShippingId = shippingId,
            ShippingName = "Standard",
            Cost = 0m,
            IsFree = true,
            MinDeliveryDays = 3,
            MaxDeliveryDays = 7
        };

        _shippingQueryService
            .CalculateShippingCostAsync(
                Arg.Any<ShippingId>(),
                Arg.Any<Money>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(
            new CalculateShippingCostQuery(shippingId, 5_000_000m),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsFree.ShouldBeTrue();
        result.Value.Cost.ShouldBe(0m);
    }

    [Fact]
    public async Task Handle_ConvertsGuidToShippingIdAndForwardsToService()
    {
        var shippingId = Guid.NewGuid();
        ShippingId? capturedShippingId = null;

        _shippingQueryService
            .CalculateShippingCostAsync(
                Arg.Do<ShippingId>(x => capturedShippingId = x),
                Arg.Any<Money>(),
                Arg.Any<CancellationToken>())
            .Returns(new ShippingCostResultDto { ShippingId = shippingId });

        await _sut.Handle(
            new CalculateShippingCostQuery(shippingId, 100_000m),
            CancellationToken.None);

        capturedShippingId.ShouldNotBeNull();
        capturedShippingId!.Value.ShouldBe(shippingId);
    }

    [Fact]
    public async Task Handle_BuildsMoneyWithDefaultIrtCurrencyAndForwardsToService()
    {
        Money? capturedMoney = null;

        _shippingQueryService
            .CalculateShippingCostAsync(
                Arg.Any<ShippingId>(),
                Arg.Do<Money>(x => capturedMoney = x),
                Arg.Any<CancellationToken>())
            .Returns(new ShippingCostResultDto());

        await _sut.Handle(
            new CalculateShippingCostQuery(Guid.NewGuid(), 320_000m),
            CancellationToken.None);

        capturedMoney.ShouldNotBeNull();
        capturedMoney!.Amount.ShouldBe(320_000m);
        capturedMoney.Currency.ShouldBe("IRT");
    }

    [Fact]
    public async Task Handle_WithZeroOrderAmount_BuildsZeroMoneyAndForwards()
    {
        Money? capturedMoney = null;

        _shippingQueryService
            .CalculateShippingCostAsync(
                Arg.Any<ShippingId>(),
                Arg.Do<Money>(x => capturedMoney = x),
                Arg.Any<CancellationToken>())
            .Returns(new ShippingCostResultDto());

        await _sut.Handle(
            new CalculateShippingCostQuery(Guid.NewGuid(), 0m),
            CancellationToken.None);

        capturedMoney.ShouldNotBeNull();
        capturedMoney!.Amount.ShouldBe(0m);
        capturedMoney.IsZero().ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToQueryService()
    {
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        _shippingQueryService
            .CalculateShippingCostAsync(
                Arg.Any<ShippingId>(),
                Arg.Any<Money>(),
                Arg.Any<CancellationToken>())
            .Returns(new ShippingCostResultDto());

        await _sut.Handle(
            new CalculateShippingCostQuery(Guid.NewGuid(), 100_000m),
            token);

        await _shippingQueryService
            .Received(1)
            .CalculateShippingCostAsync(
                Arg.Any<ShippingId>(),
                Arg.Any<Money>(),
                token);
    }

    [Fact]
    public async Task Handle_WithEmptyShippingId_ThrowsDomainException()
    {
        var query = new CalculateShippingCostQuery(Guid.Empty, 100_000m);

        await Should.ThrowAsync<DomainException>(
            () => _sut.Handle(query, CancellationToken.None));

        await _shippingQueryService
            .DidNotReceiveWithAnyArgs()
            .CalculateShippingCostAsync(default!, default!, default);
    }
}
