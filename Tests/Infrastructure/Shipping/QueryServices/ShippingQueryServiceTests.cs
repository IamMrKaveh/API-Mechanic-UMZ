using Application.Shipping.Features.Shared;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Shipping.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.Entities;
using Domain.Variant.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Shipping.QueryServices;
using Tests.TestInfrastructure.Stubs;
using Brands = Domain.Brand.Aggregates.Brand;
using Categories = Domain.Category.Aggregates.Category;
using Products = Domain.Product.Aggregates.Product;
using Shippings = Domain.Shipping.Aggregates.Shipping;

namespace Tests.Infrastructure.Shipping.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class ShippingQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private ShippingQueryService _sut = null!;

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

    private async Task<Shippings> SeedShippingAsync(
        string? name = null,
        decimal baseCost = 50_000m,
        int sortOrder = 0,
        bool isActive = true,
        bool isDefault = false,
        int minDeliveryDays = 1,
        int maxDeliveryDays = 5,
        string? estimatedDeliveryTime = null,
        string? description = null)
    {
        var effectiveName = name ?? $"Ship-{Guid.NewGuid():N}"[..15];
        var builder = new ShippingBuilder()
            .WithName(effectiveName)
            .WithBaseCost(baseCost)
            .WithDeliveryDays(minDeliveryDays, maxDeliveryDays)
            .WithEstimatedDeliveryTime(estimatedDeliveryTime)
            .WithDescription(description);

        if (isDefault)
            builder = builder.AsDefault();

        var shipping = builder.Build();

        _context.Shippings.Add(shipping);
        await _context.SaveChangesAsync();

        if (sortOrder != 0)
        {
            var entry = _context.Entry(shipping);
            entry.Property<int>("SortOrder").CurrentValue = sortOrder;
        }

        if (!isActive)
        {
            if (shipping.IsDefault)
                shipping.UnsetDefault();
            shipping.RequestDeletion();
        }

        await _context.SaveChangesAsync();
        return shipping;
    }

    private async Task<(Products product, ProductVariant variant)> SeedProductAndVariantAsync()
    {
        var catName = $"Cat-{Guid.NewGuid():N}"[..20];
        var cat = await Categories.Create(
            CategoryId.NewId(), CategoryName.Create(catName), CategorySlug.GenerateFrom(catName),
            new StubCategoryUniquenessChecker(), null, 0, CancellationToken.None);
        _context.Categories.Add(cat);
        await _context.SaveChangesAsync();

        var brandName = $"Brand-{Guid.NewGuid():N}"[..20];
        var brand = await Brands.Create(
            BrandName.Create(brandName), BrandSlug.GenerateFrom(brandName), cat.Id,
            new StubBrandUniquenessChecker(), null, null, CancellationToken.None);
        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();

        var product = new ProductBuilder().WithBrandId(brand.Id).WithCategoryId(cat.Id).Build();
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var variant = new ProductVariantBuilder().WithProductId(product.Id).Build();
        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();

        return (product, variant);
    }

    private async Task LinkVariantToShippingAsync(
        VariantId variantId,
        ShippingId shippingId,
        decimal multiplier = 1m)
    {
        var link = VariantShipping.Create(
            variantId, shippingId,
            weight: 1m, width: 10m, height: 10m, length: 10m,
            shippingMultiplier: multiplier);

        _context.VariantShippings.Add(link);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetShippingDetailAsync_WithUnknownId_ReturnsNull()
    {
        var result = await _sut.GetShippingDetailAsync(ShippingId.NewId());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetShippingDetailAsync_ReturnsMappedDto()
    {
        var shipping = await SeedShippingAsync(
            name: "پست پیشتاز",
            baseCost: 45_000m,
            estimatedDeliveryTime: "۲ تا ۵ روز",
            description: "پست پیشتاز داخل کشور",
            minDeliveryDays: 2,
            maxDeliveryDays: 5);

        var result = await _sut.GetShippingDetailAsync(shipping.Id);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(shipping.Id.Value);
        result.Name.ShouldBe("پست پیشتاز");
        result.BaseCost.ShouldBe(45_000m);
        result.Description.ShouldBe("پست پیشتاز داخل کشور");
        result.EstimatedDeliveryTime.ShouldBe("۲ تا ۵ روز");
        result.MinDeliveryDays.ShouldBe(2);
        result.MaxDeliveryDays.ShouldBe(5);
        result.IsActive.ShouldBeTrue();
        result.RowVersion.ShouldBeNull();
    }

    [Fact]
    public async Task GetAllShippingsAsync_WithNoShippings_ReturnsEmpty()
    {
        var result = await _sut.GetAllShippingsAsync();

        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAllShippingsAsync_WhenIncludeInactiveFalse_ExcludesInactive()
    {
        await SeedShippingAsync(name: "Active");
        await SeedShippingAsync(name: "Inactive", isActive: false);

        var result = await _sut.GetAllShippingsAsync(includeInactive: false);

        result.Count.ShouldBe(1);
        result[0].IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAllShippingsAsync_WhenIncludeInactiveTrue_IncludesInactive()
    {
        await SeedShippingAsync(name: "Active");
        await SeedShippingAsync(name: "Inactive", isActive: false);

        var result = await _sut.GetAllShippingsAsync(includeInactive: true);

        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetAllShippingsAsync_MapsPropertiesAndOrdersBySortOrder()
    {
        var s3 = await SeedShippingAsync(name: "Third", sortOrder: 30, baseCost: 300);
        var s1 = await SeedShippingAsync(name: "First", sortOrder: 10, baseCost: 100);
        var s2 = await SeedShippingAsync(name: "Second", sortOrder: 20, baseCost: 200);

        var result = await _sut.GetAllShippingsAsync();

        result.Count.ShouldBe(3);
        result.Select(r => r.Name).ToList().ShouldBe(new[] { "First", "Second", "Third" });
        result[0].BaseCost.ShouldBe(100);
        result[0].DeliveryTimeDisplay.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CalculateShippingCostAsync_WithUnknownShipping_ReturnsEmptyDto()
    {
        var result = await _sut.CalculateShippingCostAsync(
            ShippingId.NewId(),
            Money.Create(200_000m));

        result.ShouldNotBeNull();
        result.ShippingId.ShouldBe(Guid.Empty);
        result.ShippingName.ShouldBe(string.Empty);
        result.Cost.ShouldBe(0m);
        result.IsFree.ShouldBeFalse();
    }

    [Fact]
    public async Task CalculateShippingCostAsync_WithInactiveShipping_ReturnsEmptyDto()
    {
        var shipping = await SeedShippingAsync(isActive: false);

        var result = await _sut.CalculateShippingCostAsync(shipping.Id, Money.Create(200_000m));

        result.ShippingId.ShouldBe(Guid.Empty);
        result.Cost.ShouldBe(0m);
    }

    [Fact]
    public async Task CalculateShippingCostAsync_WithActiveShipping_ReturnsCost()
    {
        var shipping = await SeedShippingAsync(name: "Post", baseCost: 40_000m);

        var result = await _sut.CalculateShippingCostAsync(shipping.Id, Money.Create(200_000m));

        result.ShippingId.ShouldBe(shipping.Id.Value);
        result.ShippingName.ShouldBe("Post");
        result.Cost.ShouldBe(40_000m);
        result.IsFree.ShouldBeFalse();
        result.MinDeliveryDays.ShouldBe(shipping.DeliveryTime.MinDays);
        result.MaxDeliveryDays.ShouldBe(shipping.DeliveryTime.MaxDays);
    }

    [Fact]
    public async Task GetAvailableShippingsAsync_WithNoShippings_ReturnsEmpty()
    {
        var result = await _sut.GetAvailableShippingsAsync(Money.Create(100_000m));

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAvailableShippingsAsync_ReturnsOnlyActiveShippingsOrderedBySortOrder()
    {
        var second = await SeedShippingAsync(name: "Second", sortOrder: 20);
        var first = await SeedShippingAsync(name: "First", sortOrder: 10);
        await SeedShippingAsync(name: "Inactive", isActive: false);

        var result = await _sut.GetAvailableShippingsAsync(Money.Create(100_000m));

        result.Count.ShouldBe(2);
        result.Select(r => r.Name).ToList().ShouldBe(new[] { "First", "Second" });
    }

    [Fact]
    public async Task GetAvailableShippingsAsync_MapsCostAndDeliveryDisplay()
    {
        var shipping = await SeedShippingAsync(name: "Fast", baseCost: 25_000m);

        var result = await _sut.GetAvailableShippingsAsync(Money.Create(100_000m));

        result.Count.ShouldBe(1);
        var dto = result[0];
        dto.Id.ShouldBe(shipping.Id.Value);
        dto.Name.ShouldBe("Fast");
        dto.Cost.ShouldBe(25_000m);
        dto.DeliveryTimeDisplay.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetAvailableShippingsForVariantsAsync_WithEmptyVariants_ReturnsEmpty()
    {
        var result = await _sut.GetAvailableShippingsForVariantsAsync(Array.Empty<Guid>());

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAvailableShippingsForVariantsAsync_WithEmptyGuidsOnly_ReturnsEmpty()
    {
        var result = await _sut.GetAvailableShippingsForVariantsAsync(new[] { Guid.Empty, Guid.Empty });

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetAvailableShippingsForVariantsAsync_ReturnsOnlyLinkedActiveShippings()
    {
        var (_, variant) = await SeedProductAndVariantAsync();
        var linked = await SeedShippingAsync(name: "Linked", sortOrder: 1);
        var otherActive = await SeedShippingAsync(name: "OtherActive", sortOrder: 2);
        var linkedInactive = await SeedShippingAsync(name: "LinkedInactive", isActive: false);

        await LinkVariantToShippingAsync(variant.Id, linked.Id);
        await LinkVariantToShippingAsync(variant.Id, linkedInactive.Id);

        var result = await _sut.GetAvailableShippingsForVariantsAsync(new[] { variant.Id.Value });

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(linked.Id.Value);
    }

    [Fact]
    public async Task GetAvailableShippingsForVariantsAsync_UsesBaseCostAsCost_AndSetsIsFreeToFalse()
    {
        var (_, variant) = await SeedProductAndVariantAsync();
        var shipping = await SeedShippingAsync(name: "Linked", baseCost: 33_000m);
        await LinkVariantToShippingAsync(variant.Id, shipping.Id);

        var result = await _sut.GetAvailableShippingsForVariantsAsync(new[] { variant.Id.Value });

        result.Count.ShouldBe(1);
        result[0].Cost.ShouldBe(33_000m);
        result[0].IsFree.ShouldBeFalse();
    }

    [Fact]
    public async Task GetShippingQuotesAsync_WithEmptyItems_FallsBackToGetAvailableShippings()
    {
        await SeedShippingAsync(name: "GenericA", sortOrder: 1);
        await SeedShippingAsync(name: "GenericB", sortOrder: 2);

        var result = await _sut.GetShippingQuotesAsync(
            Money.Create(200_000m),
            Array.Empty<ShippingQuoteItemDto>());

        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetShippingQuotesAsync_WithItemsHavingZeroQuantity_FallsBackToAvailableShippings()
    {
        await SeedShippingAsync(name: "GenericA", sortOrder: 1);

        var items = new[]
        {
            new ShippingQuoteItemDto { VariantId = Guid.NewGuid(), Quantity = 0 }
        };

        var result = await _sut.GetShippingQuotesAsync(Money.Create(200_000m), items);

        result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetShippingQuotesAsync_WhenNoVariantsLinkedToActiveShippings_ReturnsEmpty()
    {
        await SeedShippingAsync(name: "Unrelated");

        var items = new[]
        {
            new ShippingQuoteItemDto { VariantId = Guid.NewGuid(), Quantity = 1 }
        };

        var result = await _sut.GetShippingQuotesAsync(Money.Create(200_000m), items);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetShippingQuotesAsync_ReturnsQuotesForLinkedActiveShippingsOnly()
    {
        var (_, variant) = await SeedProductAndVariantAsync();
        var linked = await SeedShippingAsync(name: "Linked", baseCost: 40_000m, sortOrder: 1);
        var linkedInactive = await SeedShippingAsync(name: "LinkedInactive", isActive: false);
        var unrelated = await SeedShippingAsync(name: "Unrelated", sortOrder: 2);

        await LinkVariantToShippingAsync(variant.Id, linked.Id, multiplier: 1m);
        await LinkVariantToShippingAsync(variant.Id, linkedInactive.Id, multiplier: 1m);

        var items = new[]
        {
            new ShippingQuoteItemDto { VariantId = variant.Id.Value, Quantity = 1 }
        };

        var result = await _sut.GetShippingQuotesAsync(Money.Create(200_000m), items);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(linked.Id.Value);
        result[0].Cost.ShouldBe(40_000m);
    }

    [Fact]
    public async Task GetShippingQuotesAsync_AppliesMultiplierAndQuantity()
    {
        var (_, variant) = await SeedProductAndVariantAsync();
        var shipping = await SeedShippingAsync(name: "MultiplierTest", baseCost: 10_000m);
        await LinkVariantToShippingAsync(variant.Id, shipping.Id, multiplier: 2m);

        var items = new[]
        {
            new ShippingQuoteItemDto { VariantId = variant.Id.Value, Quantity = 3 }
        };

        var result = await _sut.GetShippingQuotesAsync(Money.Create(200_000m), items);

        result.Count.ShouldBe(1);
        result[0].Cost.ShouldBe(60_000m);
    }
}
