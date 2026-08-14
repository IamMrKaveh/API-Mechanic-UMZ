using Application.Inventory.Contracts;
using Application.Inventory.Features.Queries.GetProductInventoryStatuses;
using Application.Inventory.Features.Shared;
using Domain.Product.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Inventory.Features.Queries.GetProductInventoryStatuses;

public class GetProductInventoryStatusesHandlerTests
{
    private readonly IInventoryQueryService _inventoryQueryService = Substitute.For<IInventoryQueryService>(); private readonly GetProductInventoryStatusesHandler _sut;

    public GetProductInventoryStatusesHandlerTests()
    {
        _sut = new GetProductInventoryStatusesHandler(_inventoryQueryService);
    }

    [Fact]
    public async Task Handle_ReturnsSuccessWithStatusesForProduct()
    {
        var productId = Guid.NewGuid();
        IReadOnlyList<InventoryStatusDto> statuses = new List<InventoryStatusDto>
    {
        new() { VariantId = Guid.NewGuid(), StockQuantity = 3, IsInStock = true }
    };

        _inventoryQueryService
            .GetInventoryStatusesByProductAsync(Arg.Any<ProductId>(), Arg.Any<CancellationToken>())
            .Returns(statuses);

        var result = await _sut.Handle(
            new GetProductInventoryStatusesQuery(productId),
            CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(statuses);
        await _inventoryQueryService.Received(1).GetInventoryStatusesByProductAsync(
            Arg.Is<ProductId>(p => p == ProductId.From(productId)),
            Arg.Any<CancellationToken>());
    }
}
