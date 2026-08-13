using Application.Shipping.Contracts;
using Application.Shipping.Features.Queries.GetShippingQuotes;
using Application.Shipping.Features.Shared;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Shipping.Features.Queries.GetShippingQuotes;

public class GetShippingQuotesHandlerTests
{
    private readonly IShippingQueryService _queryService = Substitute.For<IShippingQueryService>(); private readonly GetShippingQuotesHandler _sut;

    public GetShippingQuotesHandlerTests()
    {
        _sut = new GetShippingQuotesHandler(_queryService);
    }

    [Fact]
    public async Task Handle_WhenItemsIsNull_UsesAvailableShippingsAndPassesOrderAmount()
    {
        var expected = new List<AvailableShippingDto>
    {
        new() { Id = Guid.NewGuid(), Name = "Standard", Cost = 15_000m }
    };

        _queryService
            .GetAvailableShippingsAsync(Arg.Any<Money>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new GetShippingQuotesQuery(50_000m, null!);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);

        await _queryService.Received(1).GetAvailableShippingsAsync(
            Arg.Is<Money>(m => m == Money.Create(50_000m, "IRT")),
            Arg.Any<CancellationToken>());
        await _queryService.DidNotReceiveWithAnyArgs().GetShippingQuotesAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_WhenItemsIsEmpty_UsesAvailableShippings()
    {
        var expected = new List<AvailableShippingDto>();

        _queryService
            .GetAvailableShippingsAsync(Arg.Any<Money>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new GetShippingQuotesQuery(75_000m, []);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);

        await _queryService.Received(1).GetAvailableShippingsAsync(
            Arg.Is<Money>(m => m == Money.Create(75_000m, "IRT")),
            Arg.Any<CancellationToken>());
        await _queryService.DidNotReceiveWithAnyArgs().GetShippingQuotesAsync(default!, default!, default);
    }

    [Fact]
    public async Task Handle_ClampsNegativeOrderAmountToZeroWhenBuildingMoney()
    {
        _queryService
            .GetAvailableShippingsAsync(Arg.Any<Money>(), Arg.Any<CancellationToken>())
            .Returns(new List<AvailableShippingDto>());

        var query = new GetShippingQuotesQuery(-500m, []);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();

        await _queryService.Received(1).GetAvailableShippingsAsync(
            Arg.Is<Money>(m => m == Money.Create(0m, "IRT")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenItemsProvidedAndQuoteHasResults_ReturnsQuoteResult()
    {
        var items = new List<ShippingQuoteItemDto>
    {
        new() { VariantId = Guid.NewGuid(), Quantity = 2 }
    };
        var expected = new List<AvailableShippingDto>
    {
        new() { Id = Guid.NewGuid(), Name = "Bulk", Cost = 80_000m }
    };

        _queryService
            .GetShippingQuotesAsync(
                Arg.Any<Money>(),
                Arg.Any<IEnumerable<ShippingQuoteItemDto>>(),
                Arg.Any<CancellationToken>())
            .Returns(expected);

        var query = new GetShippingQuotesQuery(200_000m, items);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);

        await _queryService.Received(1).GetShippingQuotesAsync(
            Arg.Is<Money>(m => m == Money.Create(200_000m, "IRT")),
            items,
            Arg.Any<CancellationToken>());
        await _queryService.DidNotReceiveWithAnyArgs().GetAvailableShippingsAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenItemsProvidedAndQuoteReturnsEmpty_FallsBackToAvailableShippings()
    {
        var items = new List<ShippingQuoteItemDto>
    {
        new() { VariantId = Guid.NewGuid(), Quantity = 1 }
    };
        var fallback = new List<AvailableShippingDto>
    {
        new() { Id = Guid.NewGuid(), Name = "Fallback", Cost = 40_000m }
    };

        _queryService
            .GetShippingQuotesAsync(
                Arg.Any<Money>(),
                Arg.Any<IEnumerable<ShippingQuoteItemDto>>(),
                Arg.Any<CancellationToken>())
            .Returns(new List<AvailableShippingDto>());
        _queryService
            .GetAvailableShippingsAsync(Arg.Any<Money>(), Arg.Any<CancellationToken>())
            .Returns(fallback);

        var query = new GetShippingQuotesQuery(100_000m, items);

        var result = await _sut.Handle(query, CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(fallback);

        await _queryService.Received(1).GetShippingQuotesAsync(
            Arg.Any<Money>(),
            items,
            Arg.Any<CancellationToken>());
        await _queryService.Received(1).GetAvailableShippingsAsync(
            Arg.Is<Money>(m => m == Money.Create(100_000m, "IRT")),
            Arg.Any<CancellationToken>());
    }
}
