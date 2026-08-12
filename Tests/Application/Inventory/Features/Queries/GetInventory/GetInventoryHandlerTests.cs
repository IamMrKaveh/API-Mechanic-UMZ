using Application.Inventory.Contracts;
using Application.Inventory.Features.Queries.GetInventory;
using Application.Inventory.Features.Shared;
using Domain.Variant.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Inventory.Features.Queries.GetInventory;

public class GetInventoryHandlerTests
{
    private readonly IInventoryQueryService _inventoryQueryService = Substitute.For<IInventoryQueryService>(); private readonly GetInventoryHandler _sut;

    public GetInventoryHandlerTests()
    {
        _sut = new GetInventoryHandler(_inventoryQueryService);
    }

    [Fact]
    public async Task Handle_WhenInventoryNotFound_ReturnsNotFound()
    {
        _inventoryQueryService
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((InventoryDto?)null);

        var result = await _sut.Handle(new GetInventoryQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenInventoryExists_ReturnsSuccessWithProjectedDto()
    {
        var variantId = Guid.NewGuid();
        var source = new InventoryDto
        {
            VariantId = variantId,
            OnHand = 12,
            Reserved = 3,
            Available = 9,
            IsUnlimited = false
        };

        _inventoryQueryService
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(source);

        var result = await _sut.Handle(new GetInventoryQuery(variantId), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.VariantId.ShouldBe(variantId);
        result.Value.StockQuantity.ShouldBe(12);
        result.Value.ReservedQuantity.ShouldBe(3);
        result.Value.AvailableQuantity.ShouldBe(9);
        result.Value.IsUnlimited.ShouldBeFalse();
    }
}
