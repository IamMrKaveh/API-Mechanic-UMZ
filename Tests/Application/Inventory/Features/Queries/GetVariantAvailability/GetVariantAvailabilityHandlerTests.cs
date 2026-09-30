using Application.Inventory.Contracts;
using Application.Inventory.Features.Queries.GetVariantAvailability;
using Application.Inventory.Features.Shared;
using Domain.Variant.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Inventory.Features.Queries.GetVariantAvailability;

public class GetVariantAvailabilityHandlerTests
{
    private readonly IInventoryQueryService _inventoryQueryService = Substitute.For<IInventoryQueryService>();
    private readonly GetVariantAvailabilityHandler _sut;

    public GetVariantAvailabilityHandlerTests()
    {
        _sut = new GetVariantAvailabilityHandler(_inventoryQueryService);
    }

    [Fact]
    public async Task Handle_WhenVariantNotFound_ReturnsNotFound()
    {
        _inventoryQueryService
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((InventoryDto?)null);

        var result = await _sut.Handle(new GetVariantAvailabilityQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenInventoryExists_ReturnsMappedAvailability()
    {
        var variantId = Guid.NewGuid();
        var inventory = new InventoryDto
        {
            VariantId = variantId,
            IsInStock = true,
            AvailableStock = 4,
            IsUnlimited = false,
            IsLowStock = true
        };

        _inventoryQueryService
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.Handle(new GetVariantAvailabilityQuery(variantId), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.VariantId.ShouldBe(variantId);
        result.Value.IsAvailable.ShouldBeTrue();
        result.Value.AvailableQuantity.ShouldBe(4);
        result.Value.IsLowStock.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WhenUnlimitedInventoryReported_ReturnsSuccessWithIsAvailableTrue()
    {
        var variantId = Guid.NewGuid();
        var inventory = new InventoryDto
        {
            VariantId = variantId,
            IsInStock = false,
            IsUnlimited = true,
            AvailableStock = 0
        };

        _inventoryQueryService
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.Handle(new GetVariantAvailabilityQuery(variantId), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsAvailable.ShouldBeTrue();
        result.Value.IsUnlimited.ShouldBeTrue();
    }
}
