using Application.Shipping.Contracts;
using Application.Shipping.Features.Queries.GetAvailableShippings;
using Application.Shipping.Features.Shared;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Shipping.Features.Queries.GetAvailableShippings;

public class GetAvailableShippingsHandlerTests
{
    private readonly IShippingQueryService _queryService = Substitute.For<IShippingQueryService>(); private readonly GetAvailableShippingsHandler _sut;

    public GetAvailableShippingsHandlerTests()
    {
        _sut = new GetAvailableShippingsHandler(_queryService);
    }

    [Fact]
    public async Task Handle_DelegatesToQueryServiceWithMoneyBuiltFromOrderAmountAndReturnsSuccess()
    {
        var expected = new List<AvailableShippingDto>
    {
        new() { Id = Guid.NewGuid(), Name = "Standard", Cost = 25_000m }
    };

        _queryService
            .GetAvailableShippingsAsync(Arg.Any<Money>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetAvailableShippingsQuery(150_000m), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);

        await _queryService.Received(1).GetAvailableShippingsAsync(
            Arg.Is<Money>(m => m == Money.Create(150_000m, "IRT")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsEmptyList_ReturnsSuccessWithEmptyList()
    {
        _queryService
            .GetAvailableShippingsAsync(Arg.Any<Money>(), Arg.Any<CancellationToken>())
            .Returns(new List<AvailableShippingDto>());

        var result = await _sut.Handle(new GetAvailableShippingsQuery(0m), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeEmpty();
    }
}
