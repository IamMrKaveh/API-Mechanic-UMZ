using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;
using Infrastructure.Persistence.Context;
using Infrastructure.Variant.Repositories;
using Products = Domain.Product.Aggregates.Product;

namespace Tests.Infrastructure.Variant.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class VariantRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private IVariantRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new VariantRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<Products> PersistProductAsync()
    {
        var product = new ProductBuilder().Build();
        product.ClearDomainEvents();
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return product;
    }

    [Fact]
    public async Task AddAsync_ValidVariant_PersistsAcrossContexts()
    {
        var product = await PersistProductAsync();

        var variant = new ProductVariantBuilder()
            .WithProductId(product.Id)
            .WithSku("SKU-VAR-1")
            .WithSellingPrice(150_000m, "IRT")
            .WithOriginalPrice(200_000m, "IRT")
            .Build();
        variant.ClearDomainEvents();

        await _sut.AddAsync(variant);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new VariantRepository(freshContext);
        var loaded = await freshRepo.GetByIdAsync(variant.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(variant.Id);
        loaded.ProductId.ShouldBe(product.Id);
        loaded.Sku.Value.ShouldBe("SKU-VAR-1");
        loaded.SellingPrice.Amount.ShouldBe(150_000m);
        loaded.OriginalPrice.Amount.ShouldBe(200_000m);
        loaded.IsActive.ShouldBeTrue();
        loaded.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_WhenVariantDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(VariantId.NewId());

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdsAsync_ReturnsAllMatchingVariants()
    {
        var product = await PersistProductAsync();

        var v1 = new ProductVariantBuilder().WithProductId(product.Id).WithSku("SKU-BULK-1").Build();
        var v2 = new ProductVariantBuilder().WithProductId(product.Id).WithSku("SKU-BULK-2").Build();
        var v3 = new ProductVariantBuilder().WithProductId(product.Id).WithSku("SKU-BULK-3").Build();
        v1.ClearDomainEvents();
        v2.ClearDomainEvents();
        v3.ClearDomainEvents();

        await _sut.AddAsync(v1);
        await _sut.AddAsync(v2);
        await _sut.AddAsync(v3);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var results = await _sut.GetByIdsAsync(new[] { v1.Id, v2.Id });

        results.Count.ShouldBe(2);
        results.ShouldContain(v => v.Id == v1.Id);
        results.ShouldContain(v => v.Id == v2.Id);
        results.ShouldNotContain(v => v.Id == v3.Id);
    }

    [Fact]
    public async Task GetByIdsWithShippingsAsync_WithEmptyInput_ReturnsEmpty()
    {
        var results = await _sut.GetByIdsWithShippingsAsync(Array.Empty<VariantId>());

        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task ExistsAsync_WhenVariantExists_ReturnsTrue()
    {
        var product = await PersistProductAsync();
        var variant = new ProductVariantBuilder().WithProductId(product.Id).WithSku("SKU-EX-1").Build();
        variant.ClearDomainEvents();

        await _sut.AddAsync(variant);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.ExistsAsync(variant.Id);

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenVariantIsSoftDeleted_ReturnsFalse()
    {
        var product = await PersistProductAsync();
        var variant = new ProductVariantBuilder().WithProductId(product.Id).WithSku("SKU-EX-DEL").Build();
        variant.Remove();
        variant.ClearDomainEvents();

        await _sut.AddAsync(variant);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.ExistsAsync(variant.Id);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsBySkuAsync_MatchingSku_ReturnsTrue()
    {
        var product = await PersistProductAsync();
        var variant = new ProductVariantBuilder().WithProductId(product.Id).WithSku("SKU-EX-2").Build();
        variant.ClearDomainEvents();

        await _sut.AddAsync(variant);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.ExistsBySkuAsync(Sku.Create("SKU-EX-2"));

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsBySkuAsync_NonMatchingSku_ReturnsFalse()
    {
        var exists = await _sut.ExistsBySkuAsync(Sku.Create("SKU-MISSING"));

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task ExistsBySkuAsync_WithExcludeId_ExcludesOwnEntry()
    {
        var product = await PersistProductAsync();
        var variant = new ProductVariantBuilder().WithProductId(product.Id).WithSku("SKU-SELF").Build();
        variant.ClearDomainEvents();

        await _sut.AddAsync(variant);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.ExistsBySkuAsync(Sku.Create("SKU-SELF"), variant.Id);

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetWithProductAsync_LoadsProductNavigation()
    {
        var product = await PersistProductAsync();
        var variant = new ProductVariantBuilder().WithProductId(product.Id).WithSku("SKU-WITHPROD").Build();
        variant.ClearDomainEvents();

        await _sut.AddAsync(variant);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetWithProductAsync(variant.Id);

        loaded.ShouldNotBeNull();
        loaded!.Product.ShouldNotBeNull();
        loaded.Product.Id.ShouldBe(product.Id);
    }

    [Fact]
    public async Task Update_AfterChangePrice_PersistsNewPrice()
    {
        var product = await PersistProductAsync();
        var variant = new ProductVariantBuilder()
            .WithProductId(product.Id)
            .WithSku("SKU-PRICE-UP")
            .WithSellingPrice(100m, "IRT")
            .WithOriginalPrice(150m, "IRT")
            .Build();
        variant.ClearDomainEvents();

        await _sut.AddAsync(variant);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(variant.Id);
        reloaded.ShouldNotBeNull();
        reloaded!.ChangePrice(Money.Create(250m, "IRT"), Money.Create(300m, "IRT"));
        reloaded.ClearDomainEvents();
        _sut.Update(reloaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new VariantRepository(freshContext);
        var final = await freshRepo.GetByIdAsync(variant.Id);

        final.ShouldNotBeNull();
        final!.SellingPrice.Amount.ShouldBe(250m);
        final.OriginalPrice.Amount.ShouldBe(300m);
    }

    [Fact]
    public async Task AddAsync_DuplicateSku_ThrowsOnSaveDueToUniqueIndex()
    {
        var product = await PersistProductAsync();
        var first = new ProductVariantBuilder().WithProductId(product.Id).WithSku("SKU-DUP").Build();
        var second = new ProductVariantBuilder().WithProductId(product.Id).WithSku("SKU-DUP").Build();
        first.ClearDomainEvents();
        second.ClearDomainEvents();

        await _sut.AddAsync(first);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await _sut.AddAsync(second);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }
}
