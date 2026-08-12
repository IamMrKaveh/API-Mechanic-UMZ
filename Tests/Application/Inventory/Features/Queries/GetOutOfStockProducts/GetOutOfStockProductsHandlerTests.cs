using Application.Inventory.Contracts;
using Application.Inventory.Features.Queries.GetOutOfStockProducts;
using Application.Inventory.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Inventory.Features.Queries.GetOutOfStockProducts;

public class GetOutOfStockProductsHandlerTests
{
    private readonly IInventoryQueryService _queryService = Substitute.For<IInventoryQueryService>(); private readonly GetOutOfStockProductsHandler _sut;

    public GetOutOfStockProductsHandlerTests()
    {
        _sut = new GetOutOfStockProductsHandler(_queryService);
    }

    [Fact]
    public async Task Handle_ReturnsSuccessWithServiceResults()
    {
        var items = new List<OutOfStockItemDto>
    {
        new() { VariantId = Guid.NewGuid(), Sku = "S-1" }
    };

        _queryService
            .GetOutOfStockProductsAsync(Arg.Any<CancellationToken>())
            .Returns(items);

        var result = await _sut.Handle(new GetOutOfStockProductsQuery(), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(items);
    }
}
