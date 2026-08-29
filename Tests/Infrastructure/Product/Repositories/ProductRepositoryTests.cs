using System.Buffers.Binary;
using Domain.Product.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Product.Repositories;
using Tests.TestInfrastructure.Builders;
using Brands = Domain.Brand.Aggregates.Brand;
using Categories = Domain.Category.Aggregates.Category;

namespace Tests.Infrastructure.Product.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class ProductRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!; private ProductRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new ProductRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<(Categories Category, Brands Brand)> SeedCategoryAndBrandAsync()
    {
        var category = await new CategoryBuilder().BuildAsync();
        _context.Categories.Add(category);

        var brand = await new BrandBuilder()
            .WithCategoryId(category.Id)
            .BuildAsync();
        _context.Brands.Add(brand);

        await _context.SaveChangesAsync();

        return (category, brand);
    }

    [Fact]
    public async Task AddAsync_WithValidProduct_PersistsProduct()
    {
        var (category, brand) = await SeedCategoryAndBrandAsync();

        var product = new ProductBuilder()
            .WithName("Red Sneakers")
            .WithSlug("red-sneakers")
            .WithDescription("Comfortable red sneakers")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();

        await _sut.AddAsync(product);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var persisted = await queryContext.Products.FirstOrDefaultAsync(p => p.Id == product.Id);

        persisted.ShouldNotBeNull();
        persisted.Id.ShouldBe(product.Id);
        persisted.Name.Value.ShouldBe("Red Sneakers");
        persisted.Slug.Value.ShouldBe("red-sneakers");
        persisted.Description.ShouldBe("Comfortable red sneakers");
        persisted.BrandId.ShouldBe(brand.Id);
        persisted.CategoryId.ShouldBe(category.Id);
        persisted.IsActive.ShouldBeTrue();
        persisted.IsFeatured.ShouldBeFalse();
        persisted.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductDoesNotExist_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(ProductId.NewId());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenProductExists_ReturnsProductWithBrandAndVariantsCollection()
    {
        var (category, brand) = await SeedCategoryAndBrandAsync();

        var product = new ProductBuilder()
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new ProductRepository(queryContext);

        var result = await sut.GetByIdAsync(product.Id);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(product.Id);
        result.Brand.ShouldNotBeNull();
        result.Brand.Id.ShouldBe(brand.Id);
        result.Variants.ShouldNotBeNull();
        result.Variants.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExistsBySlugAsync_WhenSlugExists_ReturnsTrue()
    {
        var (category, brand) = await SeedCategoryAndBrandAsync();

        var slug = ProductSlug.Create("blue-shoes");
        var product = new ProductBuilder()
            .WithSlug(slug)
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new ProductRepository(queryContext);

        var result = await sut.ExistsBySlugAsync(slug);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsBySlugAsync_WhenSlugDoesNotExist_ReturnsFalse()
    {
        var (category, brand) = await SeedCategoryAndBrandAsync();

        var product = new ProductBuilder()
            .WithSlug("green-hat")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new ProductRepository(queryContext);

        var result = await sut.ExistsBySlugAsync(ProductSlug.Create("unknown-slug"));

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsBySlugAsync_WhenSlugMatchesExcludedProductId_ReturnsFalse()
    {
        var (category, brand) = await SeedCategoryAndBrandAsync();

        var slug = ProductSlug.Create("yellow-jacket");
        var product = new ProductBuilder()
            .WithSlug(slug)
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new ProductRepository(queryContext);

        var result = await sut.ExistsBySlugAsync(slug, excludeId: product.Id);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsBySlugAsync_WhenSlugExistsAndExcludedIdIsDifferent_ReturnsTrue()
    {
        var (category, brand) = await SeedCategoryAndBrandAsync();

        var existing = new ProductBuilder()
            .WithSlug("black-belt")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        _context.Products.Add(existing);

        var other = new ProductBuilder()
            .WithSlug("white-belt")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        _context.Products.Add(other);

        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new ProductRepository(queryContext);

        var result = await sut.ExistsBySlugAsync(ProductSlug.Create("black-belt"), excludeId: other.Id);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsBySlugAsync_WhenMatchingProductIsSoftDeleted_ReturnsFalse()
    {
        var (category, brand) = await SeedCategoryAndBrandAsync();

        var slug = ProductSlug.Create("deleted-slug");
        var product = new ProductBuilder()
            .WithSlug(slug)
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _context.Entry(product).Property(p => p.IsDeleted).CurrentValue = true;
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var sut = new ProductRepository(queryContext);

        var result = await sut.ExistsBySlugAsync(slug);

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task Update_WithModifiedProduct_PersistsChanges()
    {
        var (category, brand) = await SeedCategoryAndBrandAsync();

        var product = new ProductBuilder()
            .WithName("Original")
            .WithSlug("original-slug")
            .WithDescription("Original description")
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        product.UpdateDetails(
            ProductName.Create("Updated"),
            ProductSlug.Create("updated-slug"),
            "Updated description");
        product.Deactivate();
        product.MarkAsFeatured();

        _sut.Update(product);
        await _context.SaveChangesAsync();

        await using var queryContext = _fixture.CreateContext();
        var persisted = await queryContext.Products.FirstOrDefaultAsync(p => p.Id == product.Id);

        persisted.ShouldNotBeNull();
        persisted.Name.Value.ShouldBe("Updated");
        persisted.Slug.Value.ShouldBe("updated-slug");
        persisted.Description.ShouldBe("Updated description");
        persisted.IsActive.ShouldBeFalse();
        persisted.IsFeatured.ShouldBeTrue();
    }

    [Fact]
    public async Task Update_WithNullRowVersion_DoesNotSetOriginalXmin()
    {
        var (category, brand) = await SeedCategoryAndBrandAsync();

        var product = new ProductBuilder()
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var currentXmin = _context.Entry(product).Property<uint>("xmin").OriginalValue;

        _sut.Update(product, rowVersion: null);

        var afterXmin = _context.Entry(product).Property<uint>("xmin").OriginalValue;
        afterXmin.ShouldBe(currentXmin);
        _context.Entry(product).State.ShouldBe(EntityState.Modified);
    }

    [Fact]
    public async Task Update_WithFourByteRowVersion_SetsOriginalXmin()
    {
        var (category, brand) = await SeedCategoryAndBrandAsync();

        var product = new ProductBuilder()
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var buffer = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, 987654u);

        _sut.Update(product, buffer);

        var originalXmin = _context.Entry(product).Property<uint>("xmin").OriginalValue;
        originalXmin.ShouldBe(987654u);
        _context.Entry(product).State.ShouldBe(EntityState.Modified);
    }

    [Fact]
    public async Task SetOriginalRowVersion_WithEmptyRowVersion_DoesNotModifyOriginalXmin()
    {
        var (category, brand) = await SeedCategoryAndBrandAsync();

        var product = new ProductBuilder()
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var beforeXmin = _context.Entry(product).Property<uint>("xmin").OriginalValue;

        _sut.SetOriginalRowVersion(product, Array.Empty<byte>());

        var afterXmin = _context.Entry(product).Property<uint>("xmin").OriginalValue;
        afterXmin.ShouldBe(beforeXmin);
    }

    [Fact]
    public async Task SetOriginalRowVersion_WithFourByteRowVersion_SetsOriginalXmin()
    {
        var (category, brand) = await SeedCategoryAndBrandAsync();

        var product = new ProductBuilder()
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        var buffer = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(buffer, 12345u);

        _sut.SetOriginalRowVersion(product, buffer);

        var originalXmin = _context.Entry(product).Property<uint>("xmin").OriginalValue;
        originalXmin.ShouldBe(12345u);
    }
}
