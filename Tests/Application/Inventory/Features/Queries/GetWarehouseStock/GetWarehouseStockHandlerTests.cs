using Application.Inventory.Contracts;
using Application.Inventory.Features.Queries.GetWarehouseStock;
using Application.Inventory.Features.Shared;
using Domain.Variant.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Inventory.Features.Queries.GetWarehouseStock;

public class GetWarehouseStockHandlerTests
{
    private readonly IInventoryQueryService _inventoryQueryService = Substitute.For<IInventoryQueryService>(); private readonly GetWarehouseStockHandler _sut;

    public GetWarehouseStockHandlerTests()
    {
        _sut = new GetWarehouseStockHandler(_inventoryQueryService);
    }

    [Fact]
    public async Task Handle_ReturnsSuccessWithWarehouseStocks()
    {
        var variantId = Guid.NewGuid();
        var stocks = new List<WarehouseStockDto>
    {
        new() { VariantId = variantId, WarehouseId = Guid.NewGuid(), Quantity = 10 }
    };

        _inventoryQueryService
            .GetWarehouseStockByVariantAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(stocks);

        var result = await _sut.Handle(new GetWarehouseStockQuery(variantId), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(stocks);
    }
}
