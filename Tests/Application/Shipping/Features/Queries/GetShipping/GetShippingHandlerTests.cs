using Application.Shipping.Contracts;
using Application.Shipping.Features.Queries.GetShipping;
using Application.Shipping.Features.Shared;
using Domain.Shipping.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Shipping.Features.Queries.GetShipping;

public class GetShippingHandlerTests
{
    private readonly IShippingQueryService _queryService = Substitute.For<IShippingQueryService>(); private readonly GetShippingHandler _sut;

    public GetShippingHandlerTests()
    {
        _sut = new GetShippingHandler(_queryService);
    }

    [Fact]
    public async Task Handle_WhenShippingNotFound_ReturnsNotFound()
    {
        _queryService
            .GetShippingDetailAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns((ShippingDto?)null);

        var result = await _sut.Handle(new GetShippingQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenShippingExists_ReturnsSuccessWithDto()
    {
        var id = Guid.NewGuid();
        var expected = new ShippingDto { Id = id, Name = "Standard", BaseCost = 20_000m };

        _queryService
            .GetShippingDetailAsync(Arg.Any<ShippingId>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetShippingQuery(id), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);
    }

    [Fact]
    public async Task Handle_PassesShippingIdBuiltFromRequestIdToQueryService()
    {
        var id = Guid.NewGuid();
        ShippingId? captured = null;

        _queryService
            .GetShippingDetailAsync(
                Arg.Do<ShippingId>(x => captured = x),
                Arg.Any<CancellationToken>())
            .Returns((ShippingDto?)null);

        _ = await _sut.Handle(new GetShippingQuery(id), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(id);
    }
}
