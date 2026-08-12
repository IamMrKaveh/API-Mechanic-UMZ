using Application.Cache.Contracts;
using Application.Inventory.Contracts;
using Application.Inventory.Features.Queries.GetVariantAvailability;
using Application.Inventory.Features.Shared;
using Domain.Variant.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Inventory.Features.Queries.GetVariantAvailability;

public class GetVariantAvailabilityHandlerTests
{
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly IInventoryQueryService _inventoryQueryService = Substitute.For<IInventoryQueryService>();
    private readonly GetVariantAvailabilityHandler _sut;

    public GetVariantAvailabilityHandlerTests()
    {
        _sut = new GetVariantAvailabilityHandler(_cacheService, _inventoryQueryService);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ReturnsSuccessWithCachedValueAndDoesNotQueryService()
    {
        var cached = new VariantAvailabilityDto
        {
            VariantId = Guid.NewGuid(),
            IsAvailable = true,
            AvailableQuantity = 3,
            IsUnlimited = false,
            IsLowStock = false
        };

        _cacheService
            .GetAsync<VariantAvailabilityDto>(Arg.Any<string>())
            .Returns(cached);

        var result = await _sut.Handle(new GetVariantAvailabilityQuery(cached.VariantId), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.ShouldBeSameAs(cached);
        await _inventoryQueryService.DidNotReceiveWithAnyArgs().GetByVariantIdAsync(default!, default);
    }

    [Fact]
    public async Task Handle_WhenCacheMissAndVariantNotFound_ReturnsNotFound()
    {
        _cacheService
            .GetAsync<VariantAvailabilityDto>(Arg.Any<string>())
            .Returns((VariantAvailabilityDto?)null);
        _inventoryQueryService
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns((InventoryDto?)null);

        var result = await _sut.Handle(new GetVariantAvailabilityQuery(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
    }

    [Fact]
    public async Task Handle_WhenCacheMissAndInventoryExists_ReturnsSuccessAndPopulatesCache()
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

        _cacheService
            .GetAsync<VariantAvailabilityDto>(Arg.Any<string>())
            .Returns((VariantAvailabilityDto?)null);
        _inventoryQueryService
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.Handle(new GetVariantAvailabilityQuery(variantId), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.VariantId.ShouldBe(variantId);
        result.Value.IsAvailable.ShouldBeTrue();
        result.Value.AvailableQuantity.ShouldBe(4);
        result.Value.IsLowStock.ShouldBeTrue();

        await _cacheService.Received(1).SetAsync(
            Arg.Any<string>(),
            Arg.Any<VariantAvailabilityDto>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<CancellationToken>());
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

        _cacheService
            .GetAsync<VariantAvailabilityDto>(Arg.Any<string>())
            .Returns((VariantAvailabilityDto?)null);
        _inventoryQueryService
            .GetByVariantIdAsync(Arg.Any<VariantId>(), Arg.Any<CancellationToken>())
            .Returns(inventory);

        var result = await _sut.Handle(new GetVariantAvailabilityQuery(variantId), CancellationToken.None);

        result.ShouldBeSuccess();
        result.Value.IsAvailable.ShouldBeTrue();
        result.Value.IsUnlimited.ShouldBeTrue();
    }
}
