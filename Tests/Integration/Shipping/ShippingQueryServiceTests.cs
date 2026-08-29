using Application.Shipping.Features.Shared;
using Domain.Shipping.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Shipping.QueryServices;
using SharedKernel.ValueObjects;
using Shippings = Domain.Shipping.Aggregates.Shipping;

namespace Tests.Integration.Shipping;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class ShippingQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private ShippingQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new ShippingQueryService(_context);

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private static Shippings CreateShipping(
        string name = "Standard Post",
        decimal baseCost = 1000m,
        string? description = "Standard delivery method",
        string? estimatedDeliveryTime = "3 تا 5 روز کاری",
        int minDays = 3,
        int maxDays = 5)
    {
        return Shippings.Create(
            ShippingName.Create(name),
            Money.Create(baseCost),
            description,
            estimatedDeliveryTime,
            minDays,
            maxDays);
    }

    private async Task SeedAsync(params Shippings[] shippings)
    {
        foreach (var s in shippings)
            await _context.Shippings.AddAsync(s);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task GetShippingDetailAsync_WhenExists_MapsAggregateToDto()
    {
        var shipping = CreateShipping(
            name: "Express Post",
            baseCost: 2500m,
            description: "Fast delivery",
            estimatedDeliveryTime: "1 تا 2 روز کاری",
            minDays: 1,
            maxDays: 2);

        await SeedAsync(shipping);

        var result = await _sut.GetShippingDetailAsync(shipping.Id);

        result.ShouldNotBeNull();
        result!.Id.ShouldBe(shipping.Id.Value);
        result.Name.ShouldBe("Express Post");
        result.Description.ShouldBe("Fast delivery");
        result.BaseCost.ShouldBe(2500m);
        result.EstimatedDeliveryTime.ShouldBe("1 تا 2 روز کاری");
        result.MinDeliveryDays.ShouldBe(1);
        result.MaxDeliveryDays.ShouldBe(2);
        result.IsActive.ShouldBeTrue();
        result.IsDefault.ShouldBeFalse();
        result.SortOrder.ShouldBe(0);
        result.FreeShippingThreshold.ShouldBeNull();
        result.MinOrderAmount.ShouldBeNull();
        result.MaxOrderAmount.ShouldBeNull();
        result.MaxWeight.ShouldBeNull();
    }

    [Fact]
    public async Task GetShippingDetailAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _sut.GetShippingDetailAsync(ShippingId.NewId());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetAllShippingsAsync_WhenIncludeInactiveFalse_ReturnsOnlyActive()
    {
        var active = CreateShipping("Active Alpha", 500m);
        var inactive = CreateShipping("Inactive Beta", 700m);
        inactive.RequestDeletion(null);

        await SeedAsync(active, inactive);

        var result = await _sut.GetAllShippingsAsync(includeInactive: false);

        result.Count.ShouldBe(1);
        result.ShouldContain(dto => dto.Id == active.Id.Value);
        result.ShouldNotContain(dto => dto.Id == inactive.Id.Value);
    }

    [Fact]
    public async Task GetAllShippingsAsync_WhenIncludeInactiveTrue_ReturnsAllShippings()
    {
        var active = CreateShipping("Active Alpha", 500m);
        var inactive = CreateShipping("Inactive Beta", 700m);
        inactive.RequestDeletion(null);

        await SeedAsync(active, inactive);

        var result = await _sut.GetAllShippingsAsync(includeInactive: true);

        result.Count.ShouldBe(2);
        result.ShouldContain(dto => dto.Id == active.Id.Value);
        result.ShouldContain(dto => dto.Id == inactive.Id.Value);
    }

    [Fact]
    public async Task GetAllShippingsAsync_MapsCoreListItemFields()
    {
        var shipping = CreateShipping("Listing Sample", 1500m, minDays: 2, maxDays: 4);
        await SeedAsync(shipping);

        var result = await _sut.GetAllShippingsAsync();

        var item = result.Single();
        item.Id.ShouldBe(shipping.Id.Value);
        item.Name.ShouldBe("Listing Sample");
        item.BaseCost.ShouldBe(1500m);
        item.IsActive.ShouldBeTrue();
        item.IsDefault.ShouldBeFalse();
        item.SortOrder.ShouldBe(0);
        item.DeliveryTimeDisplay.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CalculateShippingCostAsync_WhenActiveShippingExists_ReturnsCostDto()
    {
        var shipping = CreateShipping("Priced Method", 1200m, minDays: 2, maxDays: 5);
        await SeedAsync(shipping);

        var result = await _sut.CalculateShippingCostAsync(shipping.Id, Money.Create(5000m));

        result.ShouldNotBeNull();
        result.ShippingId.ShouldBe(shipping.Id.Value);
        result.ShippingName.ShouldBe("Priced Method");
        result.Cost.ShouldBe(1200m);
        result.IsFree.ShouldBeFalse();
        result.MinDeliveryDays.ShouldBe(2);
        result.MaxDeliveryDays.ShouldBe(5);
    }

    [Fact]
    public async Task CalculateShippingCostAsync_WhenShippingIsInactive_ReturnsEmptyDto()
    {
        var shipping = CreateShipping("Inactive Priced", 1200m);
        shipping.RequestDeletion(null);
        await SeedAsync(shipping);

        var result = await _sut.CalculateShippingCostAsync(shipping.Id, Money.Create(5000m));

        result.ShouldNotBeNull();
        result.ShippingId.ShouldBe(Guid.Empty);
        result.Cost.ShouldBe(0m);
        result.IsFree.ShouldBeFalse();
    }

    [Fact]
    public async Task CalculateShippingCostAsync_WhenShippingNotFound_ReturnsEmptyDto()
    {
        var result = await _sut.CalculateShippingCostAsync(ShippingId.NewId(), Money.Create(5000m));

        result.ShouldNotBeNull();
        result.ShippingId.ShouldBe(Guid.Empty);
        result.Cost.ShouldBe(0m);
        result.IsFree.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAvailableShippingsAsync_ReturnsOnlyActiveShippings()
    {
        var active = CreateShipping("Available Alpha", 800m);
        var inactive = CreateShipping("Unavailable Beta", 900m);
        inactive.RequestDeletion(null);

        await SeedAsync(active, inactive);

        var result = await _sut.GetAvailableShippingsAsync(Money.Create(5000m));

        result.Count.ShouldBe(1);
        var dto = result.Single();
        dto.Id.ShouldBe(active.Id.Value);
        dto.Name.ShouldBe("Available Alpha");
        dto.Cost.ShouldBe(800m);
        dto.IsFree.ShouldBeFalse();
        dto.IsDefault.ShouldBeFalse();
        dto.DeliveryTimeDisplay.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetAvailableShippingsAsync_WhenNoActiveShippings_ReturnsEmpty()
    {
        var inactive = CreateShipping("Unavailable Only", 500m);
        inactive.RequestDeletion(null);

        await SeedAsync(inactive);

        var result = await _sut.GetAvailableShippingsAsync(Money.Create(5000m));

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAvailableShippingsForVariantsAsync_WhenVariantIdsEmpty_ReturnsEmpty()
    {
        var shipping = CreateShipping("Any Available", 500m);
        await SeedAsync(shipping);

        var result = await _sut.GetAvailableShippingsForVariantsAsync(Array.Empty<Guid>());

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAvailableShippingsForVariantsAsync_WhenNoVariantShippingMappingExists_ReturnsEmpty()
    {
        var shipping = CreateShipping("Unlinked Method", 500m);
        await SeedAsync(shipping);

        var result = await _sut.GetAvailableShippingsForVariantsAsync(new[] { Guid.NewGuid(), Guid.NewGuid() });

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetShippingQuotesAsync_WhenItemsCollectionIsEmpty_FallsBackToGetAvailableShippings()
    {
        var active = CreateShipping("Fallback Method", 600m);
        var inactive = CreateShipping("Fallback Excluded", 700m);
        inactive.RequestDeletion(null);

        await SeedAsync(active, inactive);

        var result = await _sut.GetShippingQuotesAsync(
            Money.Create(5000m),
            Array.Empty<ShippingQuoteItemDto>());

        result.Count.ShouldBe(1);
        result.ShouldContain(dto => dto.Id == active.Id.Value);
        result.ShouldNotContain(dto => dto.Id == inactive.Id.Value);
    }

    [Fact]
    public async Task GetShippingQuotesAsync_WhenAllItemsHaveNonPositiveQuantity_FallsBackToGetAvailableShippings()
    {
        var active = CreateShipping("Non Positive Fallback", 600m);
        await SeedAsync(active);

        var items = new[]
        {
        new ShippingQuoteItemDto { VariantId = Guid.NewGuid(), Quantity = 0 },
        new ShippingQuoteItemDto { VariantId = Guid.NewGuid(), Quantity = -1 }
    };

        var result = await _sut.GetShippingQuotesAsync(Money.Create(5000m), items);

        result.Count.ShouldBe(1);
        result.Single().Id.ShouldBe(active.Id.Value);
    }

    [Fact]
    public async Task GetShippingQuotesAsync_WhenNoVariantShippingRowsMatchGivenVariants_ReturnsEmpty()
    {
        var active = CreateShipping("No Mapping Method", 600m);
        await SeedAsync(active);

        var items = new[]
        {
        new ShippingQuoteItemDto { VariantId = Guid.NewGuid(), Quantity = 2 }
    };

        var result = await _sut.GetShippingQuotesAsync(Money.Create(5000m), items);

        result.ShouldBeEmpty();
    }
}
