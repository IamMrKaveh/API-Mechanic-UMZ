using Application.Inventory.Contracts;
using Application.Inventory.Features.Queries.GetLowStockProducts;
using Application.Inventory.Features.Shared;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Inventory.Features.Queries.GetLowStockProducts;

public class GetLowStockProductsHandlerTests
{
    private readonly IInventoryQueryService _queryService = Substitute.For<IInventoryQueryService>(); private readonly GetLowStockProductsHandler _sut;

    public GetLowStockProductsHandlerTests()
    {
        _sut = new GetLowStockProductsHandler(_queryService);
    }

    [Fact]
    public async Task Handle_ReturnsSuccessWithItemsFromService()
    {
        var items = new List<LowStockItemDto>
    {
        new() { ProductId = Guid.NewGuid(), VariantId = Guid.NewGuid(), StockQuantity = 2, LowStockThreshold = 5 }
    };

        _queryService
            .GetLowStockProductsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(items);

        var result = await _sut.Handle(new GetLowStockProductsQuery(5), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(items);
    }

    [Fact]
    public async Task Handle_PassesConfiguredThresholdToService()
    {
        _queryService
            .GetLowStockProductsAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<LowStockItemDto>());

        await _sut.Handle(new GetLowStockProductsQuery(3), CancellationToken.None);

        await _queryService.Received(1).GetLowStockProductsAsync(3, Arg.Any<CancellationToken>());
    }
}
