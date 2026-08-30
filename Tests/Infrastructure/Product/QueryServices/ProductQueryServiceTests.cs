using Application.Common.Contracts;
using Application.Product.Features.Shared;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;
using Domain.Variant.Aggregates;
using Infrastructure.Persistence.Context;
using Infrastructure.Product.QueryServices;
using Tests.TestInfrastructure.Stubs;
using Brands = Domain.Brand.Aggregates.Brand;
using Categories = Domain.Category.Aggregates.Category;
using Products = Domain.Product.Aggregates.Product;

namespace Tests.Infrastructure.Product.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class ProductQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private IUrlResolverService _urlResolver = null!;
    private ProductQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _urlResolver = Substitute.For<IUrlResolverService>();
        _urlResolver
            .ResolveMediaUrl(Arg.Any<string>())
            .Returns(callInfo => $"https://cdn.test/{callInfo.Arg<string>()}");

        _sut = new ProductQueryService(_context, _urlResolver);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<Categories> SeedCategoryAsync(string? name = null)
    {
        var effectiveName = name ?? $"Cat-{Guid.NewGuid():N}"[..20];
        var category = await Categories.Create(
            CategoryId.NewId(),
            CategoryName.Create(effectiveName),
            CategorySlug.GenerateFrom(effectiveName),
            new StubCategoryUniquenessChecker(),
            "test category",
            0,
            CancellationToken.None);

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    private async Task<Brands> SeedBrandAsync(CategoryId categoryId, string? name = null)
    {
        var effectiveName = name ?? $"Brand-{Guid.NewGuid():N}"[..20];
        var brand = await Brands.Create(
            BrandName.Create(effectiveName),
            BrandSlug.GenerateFrom(effectiveName),
            categoryId,
            new StubBrandUniquenessChecker(),
            "test brand",
            null,
            CancellationToken.None);

        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();
        return brand;
    }

    private async Task<Products> SeedProductAsync(
        Brands brand,
        Categories category,
        bool isActive = true,
        bool isFeatured = false,
        bool isDeleted = false,
        string? name = null)
    {
        var builder = new ProductBuilder()
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id);
        if (name is not null)
            builder = builder.WithName(name);

        var product = builder.Build();

        if (!isActive)
            product.Deactivate();

        if (isFeatured)
            product.MarkAsFeatured();

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        if (isDeleted)
        {
            product.Deactivate();
            await _context.SaveChangesAsync();
        }

        return product;
    }

    private async Task<ProductVariant> SeedVariantAsync(
        Products product,
        decimal sellingPrice = 100_000m,
        decimal originalPrice = 100_000m,
        int stock = 10,
        bool isUnlimited = false,
        bool activate = true)
    {
        var variant = new ProductVariantBuilder()
            .WithProductId(product.Id)
            .WithSellingPrice(sellingPrice)
            .WithOriginalPrice(originalPrice)
            .Build();

        if (!activate)
            variant.Deactivate();

        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync();

        var inventory = new InventoryBuilder()
            .WithVariantId(variant.Id)
            .WithInitialStock(stock);

        if (isUnlimited)
            inventory = inventory.AsUnlimited();

        var inv = inventory.Build();
        _context.Inventories.Add(inv);
        await _context.SaveChangesAsync();

        return variant;
    }

    private async Task SeedPrimaryImageAsync(Products product, string filePath = "uploads/prod/test.png")
    {
        var media = new MediaBuilder()
            .WithFilePath(filePath)
            .WithFileName("test.png")
            .WithEntityType("Product")
            .WithEntityId(product.Id.Value)
            .WithIsPrimary(false)
            .Build();

        _context.Medias.Add(media);
        await _context.SaveChangesAsync();

        media.SetAsPrimary();
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAdminProductsAsync_WithNoProducts_ReturnsEmptyResult()
    {
        var result = await _sut.GetAdminProductsAsync(
            categoryId: null, brandId: null, search: null, isActive: null,
            includeDeleted: false, page: 1, pageSize: 10);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetAdminProductsAsync_WithIncludeDeletedFalse_ExcludesSoftDeletedProducts()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        await SeedProductAsync(brand, cat);
        await SeedProductAsync(brand, cat, isDeleted: true);

        var result = await _sut.GetAdminProductsAsync(
            categoryId: null, brandId: null, search: null, isActive: null,
            includeDeleted: false, page: 1, pageSize: 20);

        result.TotalCount.ShouldBe(1);
        result.Items.ShouldAllBe(p => p.IsDeleted == false);
    }

    [Fact]
    public async Task GetAdminProductsAsync_WithIncludeDeletedTrue_IncludesSoftDeletedProducts()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        await SeedProductAsync(brand, cat);
        await SeedProductAsync(brand, cat, isDeleted: true);

        var result = await _sut.GetAdminProductsAsync(
            categoryId: null, brandId: null, search: null, isActive: null,
            includeDeleted: true, page: 1, pageSize: 20);

        result.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetAdminProductsAsync_FiltersByIsActive()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        await SeedProductAsync(brand, cat, isActive: true);
        await SeedProductAsync(brand, cat, isActive: false);

        var result = await _sut.GetAdminProductsAsync(
            categoryId: null, brandId: null, search: null, isActive: false,
            includeDeleted: false, page: 1, pageSize: 20);

        result.TotalCount.ShouldBe(1);
        result.Items[0].IsActive.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAdminProductsAsync_FiltersByBrandId()
    {
        var cat = await SeedCategoryAsync();
        var brandA = await SeedBrandAsync(cat.Id);
        var brandB = await SeedBrandAsync(cat.Id);
        await SeedProductAsync(brandA, cat);
        await SeedProductAsync(brandA, cat);
        await SeedProductAsync(brandB, cat);

        var result = await _sut.GetAdminProductsAsync(
            categoryId: null, brandId: brandA.Id.Value, search: null, isActive: null,
            includeDeleted: false, page: 1, pageSize: 20);

        result.TotalCount.ShouldBe(2);
        result.Items.ShouldAllBe(p => p.BrandId == brandA.Id.Value);
    }

    [Fact]
    public async Task GetAdminProductsAsync_FiltersByCategoryId()
    {
        var catA = await SeedCategoryAsync();
        var catB = await SeedCategoryAsync();
        var brandA = await SeedBrandAsync(catA.Id);
        var brandB = await SeedBrandAsync(catB.Id);
        await SeedProductAsync(brandA, catA);
        await SeedProductAsync(brandB, catB);

        var result = await _sut.GetAdminProductsAsync(
            categoryId: catB.Id.Value, brandId: null, search: null, isActive: null,
            includeDeleted: false, page: 1, pageSize: 20);

        result.TotalCount.ShouldBe(1);
        result.Items[0].CategoryId.ShouldBe(catB.Id.Value);
    }

    [Fact]
    public async Task GetAdminProductsAsync_SearchByName_UsesCaseInsensitiveILike()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        await SeedProductAsync(brand, cat, name: "HeadPhone Wireless");
        await SeedProductAsync(brand, cat, name: "Bluetooth Speaker");

        var result = await _sut.GetAdminProductsAsync(
            categoryId: null, brandId: null, search: "headphone", isActive: null,
            includeDeleted: false, page: 1, pageSize: 20);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Name.ShouldBe("HeadPhone Wireless");
    }

    [Fact]
    public async Task GetAdminProductsAsync_OrdersByCreatedAtDescending()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        var p1 = await SeedProductAsync(brand, cat, name: "First");
        await Task.Delay(20);
        var p2 = await SeedProductAsync(brand, cat, name: "Second");
        await Task.Delay(20);
        var p3 = await SeedProductAsync(brand, cat, name: "Third");

        var result = await _sut.GetAdminProductsAsync(
            categoryId: null, brandId: null, search: null, isActive: null,
            includeDeleted: false, page: 1, pageSize: 20);

        result.Items.Select(x => x.Id).ToList().ShouldBe(new[] { p3.Id.Value, p2.Id.Value, p1.Id.Value });
    }

    [Fact]
    public async Task GetAdminProductsAsync_ComputesMinPriceAndTotalStockAcrossVariants()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        var product = await SeedProductAsync(brand, cat);
        await SeedVariantAsync(product, sellingPrice: 250_000m, stock: 3);
        await SeedVariantAsync(product, sellingPrice: 150_000m, stock: 7);

        var result = await _sut.GetAdminProductsAsync(
            categoryId: null, brandId: null, search: null, isActive: null,
            includeDeleted: false, page: 1, pageSize: 20);

        result.Items.Count.ShouldBe(1);
        result.Items[0].MinPrice.ShouldBe(150_000m);
        result.Items[0].TotalStock.ShouldBe(10);
        result.Items[0].HasStock.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAdminProductsAsync_HasStockTrue_WhenAnyVariantIsUnlimited()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        var product = await SeedProductAsync(brand, cat);
        await SeedVariantAsync(product, stock: 0, isUnlimited: true);

        var result = await _sut.GetAdminProductsAsync(
            categoryId: null, brandId: null, search: null, isActive: null,
            includeDeleted: false, page: 1, pageSize: 20);

        result.Items[0].HasStock.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAdminProductsAsync_WithPrimaryImage_ResolvesUrl()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        var product = await SeedProductAsync(brand, cat);
        await SeedPrimaryImageAsync(product, "uploads/prod/img.png");

        var result = await _sut.GetAdminProductsAsync(
            categoryId: null, brandId: null, search: null, isActive: null,
            includeDeleted: false, page: 1, pageSize: 20);

        result.Items[0].PrimaryImageUrl.ShouldBe("https://cdn.test/uploads/prod/img.png");
        _urlResolver.Received().ResolveMediaUrl("uploads/prod/img.png");
    }

    [Fact]
    public async Task GetAdminProductsAsync_WithNoPrimaryImage_LeavesUrlNull()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        await SeedProductAsync(brand, cat);

        var result = await _sut.GetAdminProductsAsync(
            categoryId: null, brandId: null, search: null, isActive: null,
            includeDeleted: false, page: 1, pageSize: 20);

        result.Items[0].PrimaryImageUrl.ShouldBeNull();
    }

    [Fact]
    public async Task GetProductDetailAsync_WithUnknownId_ReturnsNull()
    {
        var result = await _sut.GetProductDetailAsync(ProductId.NewId());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetProductDetailAsync_WithSoftDeletedProduct_ReturnsNull()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        var product = await SeedProductAsync(brand, cat, isDeleted: true);

        var result = await _sut.GetProductDetailAsync(product.Id);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetProductDetailAsync_ReturnsMappedDtoWithBrandAndCategoryNames()
    {
        var cat = await SeedCategoryAsync("CategoryX");
        var brand = await SeedBrandAsync(cat.Id, "BrandX");
        var product = await SeedProductAsync(brand, cat, name: "ProductX");
        await SeedVariantAsync(product, sellingPrice: 200_000m, originalPrice: 250_000m, stock: 15);

        var result = await _sut.GetProductDetailAsync(product.Id);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(product.Id.Value);
        result.Name.ShouldBe("ProductX");
        result.BrandId.ShouldBe(brand.Id.Value);
        result.BrandName.ShouldBe("BrandX");
        result.CategoryId.ShouldBe(cat.Id.Value);
        result.CategoryName.ShouldBe("CategoryX");
        result.Variants.Count.ShouldBe(1);
        result.Variants[0].SellingPrice.ShouldBe(200_000m);
        result.Variants[0].OriginalPrice.ShouldBe(250_000m);
        result.Variants[0].HasDiscount.ShouldBeTrue();
        result.Variants[0].StockQuantity.ShouldBe(15);
        result.Variants[0].IsInStock.ShouldBeTrue();
    }

    [Fact]
    public async Task GetProductDetailAsync_ProjectsRowVersionAsBase64()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        var product = await SeedProductAsync(brand, cat);

        var result = await _sut.GetProductDetailAsync(product.Id);

        result.ShouldNotBeNull();
        result.RowVersion.ShouldNotBeNullOrWhiteSpace();
        Convert.FromBase64String(result.RowVersion).Length.ShouldBe(4);
    }

    [Fact]
    public async Task GetProductDetailAsync_ResolvesPrimaryImageUrlWhenPresent()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        var product = await SeedProductAsync(brand, cat);
        await SeedPrimaryImageAsync(product, "uploads/prod/detail.png");

        var result = await _sut.GetProductDetailAsync(product.Id);

        result.ShouldNotBeNull();
        result.PrimaryImageUrl.ShouldBe("https://cdn.test/uploads/prod/detail.png");
    }

    [Fact]
    public async Task GetProductCatalogAsync_ReturnsOnlyActiveAndNonDeletedProducts()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        var active = await SeedProductAsync(brand, cat);
        await SeedProductAsync(brand, cat, isActive: false);
        await SeedProductAsync(brand, cat, isDeleted: true);
        await SeedVariantAsync(active, sellingPrice: 100_000m, stock: 5);

        var searchParams = new ProductCatalogSearchParams(
            Page: 1, PageSize: 20, Search: null, CategoryId: null, BrandId: null,
            MinPrice: null, MaxPrice: null, InStockOnly: false, SortBy: null,
            IsFeatured: null, HasDiscount: null);

        var result = await _sut.GetProductCatalogAsync(searchParams);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(active.Id.Value);
    }

    [Fact]
    public async Task GetProductCatalogAsync_FiltersByIsFeatured()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        await SeedProductAsync(brand, cat, isFeatured: true);
        await SeedProductAsync(brand, cat, isFeatured: false);

        var searchParams = new ProductCatalogSearchParams(
            Page: 1, PageSize: 20, Search: null, CategoryId: null, BrandId: null,
            MinPrice: null, MaxPrice: null, InStockOnly: false, SortBy: null,
            IsFeatured: true, HasDiscount: null);

        var result = await _sut.GetProductCatalogAsync(searchParams);

        result.TotalCount.ShouldBe(1);
        result.Items[0].IsFeatured.ShouldBeTrue();
    }

    [Fact]
    public async Task GetProductCatalogAsync_FiltersByPriceRange()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        var cheap = await SeedProductAsync(brand, cat, name: "Cheap");
        var expensive = await SeedProductAsync(brand, cat, name: "Pricey");
        await SeedVariantAsync(cheap, sellingPrice: 50_000m, stock: 5);
        await SeedVariantAsync(expensive, sellingPrice: 500_000m, stock: 5);

        var searchParams = new ProductCatalogSearchParams(
            Page: 1, PageSize: 20, Search: null, CategoryId: null, BrandId: null,
            MinPrice: 100_000m, MaxPrice: 1_000_000m, InStockOnly: false, SortBy: null,
            IsFeatured: null, HasDiscount: null);

        var result = await _sut.GetProductCatalogAsync(searchParams);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(expensive.Id.Value);
    }

    [Fact]
    public async Task GetProductCatalogAsync_InStockOnly_ExcludesProductsWithNoStock()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        var inStock = await SeedProductAsync(brand, cat, name: "InStock");
        var outOfStock = await SeedProductAsync(brand, cat, name: "OutOfStock");
        await SeedVariantAsync(inStock, stock: 5);
        await SeedVariantAsync(outOfStock, stock: 0);

        var searchParams = new ProductCatalogSearchParams(
            Page: 1, PageSize: 20, Search: null, CategoryId: null, BrandId: null,
            MinPrice: null, MaxPrice: null, InStockOnly: true, SortBy: null,
            IsFeatured: null, HasDiscount: null);

        var result = await _sut.GetProductCatalogAsync(searchParams);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(inStock.Id.Value);
    }

    [Fact]
    public async Task GetProductCatalogAsync_HasDiscountTrue_ReturnsOnlyDiscountedProducts()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        var discounted = await SeedProductAsync(brand, cat, name: "Discounted");
        var full = await SeedProductAsync(brand, cat, name: "Full");
        await SeedVariantAsync(discounted, sellingPrice: 80_000m, originalPrice: 100_000m, stock: 5);
        await SeedVariantAsync(full, sellingPrice: 100_000m, originalPrice: 100_000m, stock: 5);

        var searchParams = new ProductCatalogSearchParams(
            Page: 1, PageSize: 20, Search: null, CategoryId: null, BrandId: null,
            MinPrice: null, MaxPrice: null, InStockOnly: false, SortBy: null,
            IsFeatured: null, HasDiscount: true);

        var result = await _sut.GetProductCatalogAsync(searchParams);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(discounted.Id.Value);
        result.Items[0].HasDiscount.ShouldBeTrue();
        result.Items[0].DiscountPercentage.ShouldBe(20);
    }

    [Fact]
    public async Task GetProductCatalogAsync_HasDiscountFalse_ExcludesDiscountedProducts()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        var discounted = await SeedProductAsync(brand, cat, name: "Discounted");
        var full = await SeedProductAsync(brand, cat, name: "Full");
        await SeedVariantAsync(discounted, sellingPrice: 80_000m, originalPrice: 100_000m, stock: 5);
        await SeedVariantAsync(full, sellingPrice: 100_000m, originalPrice: 100_000m, stock: 5);

        var searchParams = new ProductCatalogSearchParams(
            Page: 1, PageSize: 20, Search: null, CategoryId: null, BrandId: null,
            MinPrice: null, MaxPrice: null, InStockOnly: false, SortBy: null,
            IsFeatured: null, HasDiscount: false);

        var result = await _sut.GetProductCatalogAsync(searchParams);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Id.ShouldBe(full.Id.Value);
    }

    [Theory]
    [InlineData("price_asc")]
    [InlineData("price_desc")]
    [InlineData("name_asc")]
    [InlineData("name_desc")]
    [InlineData("featured")]
    [InlineData("newest")]
    public async Task GetProductCatalogAsync_ReturnsResultsForVariousSortModes(string sortBy)
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        var pA = await SeedProductAsync(brand, cat, name: "Aardvark");
        var pB = await SeedProductAsync(brand, cat, name: "Zebra", isFeatured: true);
        await SeedVariantAsync(pA, sellingPrice: 50_000m, stock: 5);
        await SeedVariantAsync(pB, sellingPrice: 500_000m, stock: 5);

        var searchParams = new ProductCatalogSearchParams(
            Page: 1, PageSize: 20, Search: null, CategoryId: null, BrandId: null,
            MinPrice: null, MaxPrice: null, InStockOnly: false, SortBy: sortBy,
            IsFeatured: null, HasDiscount: null);

        var result = await _sut.GetProductCatalogAsync(searchParams);

        result.TotalCount.ShouldBe(2);

        switch (sortBy)
        {
            case "price_asc":
                result.Items[0].Id.ShouldBe(pA.Id.Value);
                break;

            case "price_desc":
                result.Items[0].Id.ShouldBe(pB.Id.Value);
                break;

            case "name_asc":
                result.Items[0].Name.ShouldBe("Aardvark");
                break;

            case "name_desc":
                result.Items[0].Name.ShouldBe("Zebra");
                break;

            case "featured":
                result.Items[0].Id.ShouldBe(pB.Id.Value);
                break;
        }
    }

    [Fact]
    public async Task GetProductCatalogAsync_ComputesDiscountPercentageCorrectly()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        var product = await SeedProductAsync(brand, cat);
        await SeedVariantAsync(product, sellingPrice: 75_000m, originalPrice: 100_000m, stock: 5);

        var searchParams = new ProductCatalogSearchParams(
            Page: 1, PageSize: 20, Search: null, CategoryId: null, BrandId: null,
            MinPrice: null, MaxPrice: null, InStockOnly: false, SortBy: null,
            IsFeatured: null, HasDiscount: null);

        var result = await _sut.GetProductCatalogAsync(searchParams);

        result.Items[0].DiscountPercentage.ShouldBe(25);
        result.Items[0].HasDiscount.ShouldBeTrue();
        result.Items[0].OriginalPrice.ShouldBe(100_000m);
    }

    [Fact]
    public async Task GetProductCatalogAsync_ProductWithNoActiveVariants_HasNullMinPrice()
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        var product = await SeedProductAsync(brand, cat);
        await SeedVariantAsync(product, activate: false);

        var searchParams = new ProductCatalogSearchParams(
            Page: 1, PageSize: 20, Search: null, CategoryId: null, BrandId: null,
            MinPrice: null, MaxPrice: null, InStockOnly: false, SortBy: null,
            IsFeatured: null, HasDiscount: null);

        var result = await _sut.GetProductCatalogAsync(searchParams);

        result.TotalCount.ShouldBe(1);
        result.Items[0].MinPrice.ShouldBeNull();
        result.Items[0].HasDiscount.ShouldBeFalse();
    }

    [Theory]
    [InlineData(1, 2, 2, 5)]
    [InlineData(3, 2, 1, 5)]
    [InlineData(1, 10, 5, 5)]
    public async Task GetProductCatalogAsync_PaginatesCorrectly(int page, int pageSize, int expected, int total)
    {
        var cat = await SeedCategoryAsync();
        var brand = await SeedBrandAsync(cat.Id);
        for (var i = 0; i < total; i++)
        {
            var product = await SeedProductAsync(brand, cat, name: $"Product-{i}");
            await SeedVariantAsync(product, sellingPrice: 10_000m + i * 1_000m, stock: 5);
        }

        var searchParams = new ProductCatalogSearchParams(
            Page: page, PageSize: pageSize, Search: null, CategoryId: null, BrandId: null,
            MinPrice: null, MaxPrice: null, InStockOnly: false, SortBy: null,
            IsFeatured: null, HasDiscount: null);

        var result = await _sut.GetProductCatalogAsync(searchParams);

        result.Items.Count.ShouldBe(expected);
        result.TotalCount.ShouldBe(total);
    }
}
