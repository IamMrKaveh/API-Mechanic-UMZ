using Application.Shipping.Contracts;
using Application.Shipping.Features.Queries.GetShippings;
using Application.Shipping.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Shipping.Features.Queries.GetShippings;

public class GetShippingsHandlerTests
{
    private readonly IShippingQueryService _queryService = Substitute.For<IShippingQueryService>(); private readonly GetShippingsHandler _sut;

    public GetShippingsHandlerTests()
    {
        _sut = new GetShippingsHandler(_queryService);
    }

    [Fact]
    public async Task Handle_WithDefaultQuery_PassesIncludeInactiveFalseAndReturnsSuccess()
    {
        var expected = new List<ShippingListItemDto>
    {
        new() { Id = Guid.NewGuid(), Name = "Standard", BaseCost = 25_000m, IsActive = true }
    };

        _queryService
            .GetAllShippingsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetShippingsQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);

        await _queryService.Received(1).GetAllShippingsAsync(false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithIncludeInactiveTrue_PassesIncludeInactiveTrue()
    {
        _queryService
            .GetAllShippingsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<ShippingListItemDto>());

        var result = await _sut.Handle(new GetShippingsQuery(IncludeInactive: true), CancellationToken.None);

        result.ShouldBeSuccess();
        await _queryService.Received(1).GetAllShippingsAsync(true, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsEmpty_ReturnsSuccessWithEmptyList()
    {
        _queryService
            .GetAllShippingsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new List<ShippingListItemDto>());

        var result = await _sut.Handle(new GetShippingsQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeEmpty();
    }
}
