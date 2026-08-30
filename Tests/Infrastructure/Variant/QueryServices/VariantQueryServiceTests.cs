using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Variant.QueryServices;
using ProductVariants = Domain.Variant.Aggregates.ProductVariant;

namespace Tests.Infrastructure.Variant.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class VariantQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private VariantQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new VariantQueryService(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable) return;
        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<ProductId> SeedProductAsync()
    {
        var category = await new CategoryBuilder()
            .WithName($"cat-{Guid.NewGuid():N}")
            .WithSlug($"cat-{Guid.NewGuid():N}")
            .BuildAsync();
        _context.Categories.Add(category);

        var brand = await new BrandBuilder()
            .WithName($"brand-{Guid.NewGuid():N}")
            .WithSlug($"brand-{Guid.NewGuid():N}")
            .WithCategoryId(category.Id)
            .BuildAsync();
        _context.Brands.Add(brand);

        var product = new ProductBuilder()
            .WithName($"prod-{Guid.NewGuid():N}")
            .WithSlug($"prod-{Guid.NewGuid():N}")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        product.ClearDomainEvents();
        _context.Products.Add(product);

        await _context.SaveChangesAsync();
        return product.Id;
    }

    private ProductVariants BuildVariant(ProductId productId, decimal sellingPrice = 100_000m, decimal? originalPrice = null, string? sku = null)
    {
        var builder = new ProductVariantBuilder()
            .WithProductId(productId)
            .WithSku(sku ?? $"SKU-{Guid.NewGuid():N}"[..20])
            .WithSellingPrice(sellingPrice);

        if (originalPrice.HasValue)
            builder = builder.WithOriginalPrice(originalPrice.Value);

        var variant = builder.Build();
        variant.ClearDomainEvents();
        return variant;
    }

    [Fact]
    public async Task GetProductVariantsAsync_WhenProductHasNoVariants_ReturnsEmpty()
    {
        var productId = await SeedProductAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetProductVariantsAsync(productId, activeOnly: false, CancellationToken.None);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetProductVariantsAsync_WhenVariantsExist_ReturnsMappedDtosWithInventoryStock()
    {
        var productId = await SeedProductAsync();
        var variant = BuildVariant(productId, sellingPrice: 250_000m, originalPrice: 300_000m);
        _context.ProductVariants.Add(variant);

        var inventory = new InventoryBuilder()
            .WithVariantId(variant.Id)
            .WithInitialStock(15)
            .Build();
        inventory.ClearDomainEvents();
        _context.Inventories.Add(inventory);

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = (await _sut.GetProductVariantsAsync(productId, activeOnly: false, CancellationToken.None)).ToList();

        result.Count.ShouldBe(1);
        var dto = result[0];
        dto.Id.ShouldBe(variant.Id.Value);
        dto.SellingPrice.ShouldBe(250_000m);
        dto.OriginalPrice.ShouldBe(300_000m);
        dto.StockQuantity.ShouldBe(15);
        dto.Stock.ShouldBe(15);
        dto.IsInStock.ShouldBeTrue();
        dto.IsUnlimited.ShouldBeFalse();
        dto.IsActive.ShouldBeTrue();
        dto.ShippingMultiplier.ShouldBe(1m);
        dto.EnabledShippingIds.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetProductVariantsAsync_WhenNoInventoryRow_ReturnsZeroStockAndNotInStock()
    {
        var productId = await SeedProductAsync();
        var variant = BuildVariant(productId);
        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = (await _sut.GetProductVariantsAsync(productId, activeOnly: false, CancellationToken.None)).ToList();

        result.Count.ShouldBe(1);
        result[0].Stock.ShouldBe(0);
        result[0].IsInStock.ShouldBeFalse();
        result[0].IsUnlimited.ShouldBeFalse();
    }

    [Fact]
    public async Task GetProductVariantsAsync_WhenInventoryIsUnlimited_MapsIsUnlimitedAndIsInStock()
    {
        var productId = await SeedProductAsync();
        var variant = BuildVariant(productId);
        _context.ProductVariants.Add(variant);

        var inventory = new InventoryBuilder()
            .WithVariantId(variant.Id)
            .AsUnlimited()
            .Build();
        inventory.ClearDomainEvents();
        _context.Inventories.Add(inventory);

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = (await _sut.GetProductVariantsAsync(productId, activeOnly: false, CancellationToken.None)).ToList();

        result[0].IsUnlimited.ShouldBeTrue();
        result[0].IsInStock.ShouldBeTrue();
    }

    [Fact]
    public async Task GetProductVariantsAsync_WhenActiveOnlyIsTrue_ExcludesInactiveVariants()
    {
        var productId = await SeedProductAsync();
        var active = BuildVariant(productId, sku: $"ACT-{Guid.NewGuid():N}"[..20]);
        var inactive = BuildVariant(productId, sku: $"INA-{Guid.NewGuid():N}"[..20]);
        inactive.Deactivate();
        inactive.ClearDomainEvents();

        _context.ProductVariants.AddRange(active, inactive);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = (await _sut.GetProductVariantsAsync(productId, activeOnly: true, CancellationToken.None)).ToList();

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(active.Id.Value);
    }

    [Fact]
    public async Task GetProductVariantsAsync_WhenActiveOnlyIsFalse_IncludesInactiveVariants()
    {
        var productId = await SeedProductAsync();
        var active = BuildVariant(productId, sku: $"A-{Guid.NewGuid():N}"[..20]);
        var inactive = BuildVariant(productId, sku: $"I-{Guid.NewGuid():N}"[..20]);
        inactive.Deactivate();
        inactive.ClearDomainEvents();

        _context.ProductVariants.AddRange(active, inactive);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = (await _sut.GetProductVariantsAsync(productId, activeOnly: false, CancellationToken.None)).ToList();

        result.Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetProductVariantsAsync_ExcludesSoftDeletedVariants()
    {
        var productId = await SeedProductAsync();
        var visible = BuildVariant(productId, sku: $"V-{Guid.NewGuid():N}"[..20]);
        var deleted = BuildVariant(productId, sku: $"D-{Guid.NewGuid():N}"[..20]);
        deleted.Remove();
        deleted.ClearDomainEvents();

        _context.ProductVariants.AddRange(visible, deleted);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = (await _sut.GetProductVariantsAsync(productId, activeOnly: false, CancellationToken.None)).ToList();

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(visible.Id.Value);
    }

    [Fact]
    public async Task GetProductVariantsAsync_OnlyReturnsVariantsForRequestedProduct()
    {
        var product1 = await SeedProductAsync();
        var product2 = await SeedProductAsync();

        _context.ProductVariants.Add(BuildVariant(product1, sku: $"P1-{Guid.NewGuid():N}"[..20]));
        _context.ProductVariants.Add(BuildVariant(product2, sku: $"P2-{Guid.NewGuid():N}"[..20]));
        _context.ProductVariants.Add(BuildVariant(product2, sku: $"P3-{Guid.NewGuid():N}"[..20]));

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var resultForProduct2 = await _sut.GetProductVariantsAsync(product2, activeOnly: false, CancellationToken.None);

        resultForProduct2.Count().ShouldBe(2);
    }

    [Fact]
    public async Task GetVariantShippingInfoAsync_WhenVariantDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetVariantShippingInfoAsync(VariantId.NewId(), CancellationToken.None);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetVariantShippingInfoAsync_WhenVariantExistsWithoutShippings_ReturnsDefaultMultiplierAndEmptyLists()
    {
        var productId = await SeedProductAsync();
        var variant = BuildVariant(productId);
        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetVariantShippingInfoAsync(variant.Id, CancellationToken.None);

        result.ShouldNotBeNull();
        result!.VariantId.ShouldBe(variant.Id.Value);
        result.ShippingMultiplier.ShouldBe(1m);
        result.WeightGrams.ShouldBe(0m);
        result.EnabledShippingIds.ShouldBeEmpty();
        result.AvailableShippings.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetVariantShippingInfoAsync_WhenVariantIsSoftDeleted_ReturnsNull()
    {
        var productId = await SeedProductAsync();
        var variant = BuildVariant(productId);
        variant.Remove();
        variant.ClearDomainEvents();
        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetVariantShippingInfoAsync(variant.Id, CancellationToken.None);

        result.ShouldBeNull();
    }
}
