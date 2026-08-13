using Application.Shipping.Contracts;
using Application.Shipping.Features.Queries.GetAvailableShippingsForVariants;
using Application.Shipping.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Shipping.Features.Queries.GetAvailableShippingsForVariants;

public class GetAvailableShippingsForVariantsHandlerTests
{
    private readonly IShippingQueryService _queryService = Substitute.For<IShippingQueryService>(); private readonly GetAvailableShippingsForVariantsHandler _sut;

    public GetAvailableShippingsForVariantsHandlerTests()
    {
        _sut = new GetAvailableShippingsForVariantsHandler(_queryService);
    }

    [Fact]
    public async Task Handle_DelegatesVariantIdsToQueryServiceAndReturnsSuccess()
    {
        var variantIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var expected = new List<AvailableShippingDto>
    {
        new() { Id = Guid.NewGuid(), Name = "Standard", Cost = 30_000m }
    };

        _queryService
            .GetAvailableShippingsForVariantsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await _sut.Handle(new GetAvailableShippingsForVariantsQuery(variantIds), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBe(expected);

        await _queryService.Received(1).GetAvailableShippingsForVariantsAsync(
            variantIds,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenServiceReturnsEmptyList_ReturnsSuccessWithEmptyList()
    {
        _queryService
            .GetAvailableShippingsForVariantsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<AvailableShippingDto>());

        var result = await _sut.Handle(
            new GetAvailableShippingsForVariantsQuery(new List<Guid> { Guid.NewGuid() }),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeEmpty();
    }
}
