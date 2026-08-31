using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Inventory.Interfaces;
using Domain.Inventory.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.ValueObjects;
using Infrastructure.Inventory.Repositories;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.Inventory.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class InventoryRepositoryTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private IInventoryRepository _sut = null!;
    private BrandId _brandId = null!;
    private CategoryId _categoryId = null!;

    protected override async Task OnInitializeAsync()
    {
        _sut = new InventoryRepository(Context);
        var (brand, category) = await SeedBrandWithCategoryAsync();
        _brandId = brand.Id;
        _categoryId = category.Id;
    }

    private async Task<ProductVariant> PersistVariantAsync(string skuValue)
    {
        var product = new ProductBuilder()
            .WithBrandId(_brandId)
            .WithCategoryId(_categoryId)
            .Build();
        product.ClearDomainEvents();
        await Context.Products.AddAsync(product);

        var variant = new ProductVariantBuilder()
            .WithProductId(product.Id)
            .WithSku(skuValue)
            .Build();
        variant.ClearDomainEvents();
        await Context.ProductVariants.AddAsync(variant);

        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        return variant;
    }

    [Fact]
    public async Task AddAsync_ValidInventory_PersistsAcrossContexts()
    {
        var variant = await PersistVariantAsync("SKU-INV-1");
        var inventory = new InventoryBuilder()
            .WithVariantId(variant.Id)
            .WithInitialStock(50)
            .WithLowStockThreshold(5)
            .Build();
        inventory.ClearDomainEvents();

        await _sut.AddAsync(inventory);
        await Context.SaveChangesAsync();

        await using var freshContext = Fixture.CreateContext();
        var freshRepo = new InventoryRepository(freshContext);
        var loaded = await freshRepo.GetByIdAsync(inventory.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(inventory.Id);
        loaded.VariantId.ShouldBe(variant.Id);
        loaded.StockQuantity.Value.ShouldBe(50);
        loaded.ReservedQuantity.Value.ShouldBe(0);
        loaded.LowStockThreshold.ShouldBe(5);
        loaded.IsUnlimited.ShouldBeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_WhenInventoryDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(InventoryId.NewId());

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task GetByVariantIdAsync_WhenExists_ReturnsInventory()
    {
        var variant = await PersistVariantAsync("SKU-INV-2");
        var inventory = new InventoryBuilder()
            .WithVariantId(variant.Id)
            .WithInitialStock(10)
            .Build();
        inventory.ClearDomainEvents();

        await _sut.AddAsync(inventory);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var loaded = await _sut.GetByVariantIdAsync(variant.Id);

        loaded.ShouldNotBeNull();
        loaded!.VariantId.ShouldBe(variant.Id);
        loaded.StockQuantity.Value.ShouldBe(10);
    }

    [Fact]
    public async Task GetByVariantIdAsync_WhenNoInventoryForVariant_ReturnsNull()
    {
        var loaded = await _sut.GetByVariantIdAsync(VariantId.NewId());

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task GetByVariantIdWithLedgerAsync_WhenInventoryHasLedgerEntries_LoadsThem()
    {
        var variant = await PersistVariantAsync("SKU-INV-3");
        var inventory = new InventoryBuilder()
            .WithVariantId(variant.Id)
            .WithInitialStock(20)
            .Build();
        inventory.ClearDomainEvents();

        await _sut.AddAsync(inventory);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var loaded = await _sut.GetByVariantIdWithLedgerAsync(variant.Id);

        loaded.ShouldNotBeNull();
        loaded!.LedgerEntries.Count.ShouldBeGreaterThanOrEqualTo(1);
        loaded.LedgerEntries.ShouldContain(e => e.QuantityDelta == 20);
    }

    [Fact]
    public async Task GetByVariantIdsAsync_ReturnsAllMatchingInventories()
    {
        var variantA = await PersistVariantAsync("SKU-INV-A");
        var variantB = await PersistVariantAsync("SKU-INV-B");
        var variantC = await PersistVariantAsync("SKU-INV-C");

        var invA = new InventoryBuilder().WithVariantId(variantA.Id).WithInitialStock(5).Build();
        var invB = new InventoryBuilder().WithVariantId(variantB.Id).WithInitialStock(10).Build();
        var invC = new InventoryBuilder().WithVariantId(variantC.Id).WithInitialStock(15).Build();
        invA.ClearDomainEvents();
        invB.ClearDomainEvents();
        invC.ClearDomainEvents();

        await _sut.AddAsync(invA);
        await _sut.AddAsync(invB);
        await _sut.AddAsync(invC);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var results = await _sut.GetByVariantIdsAsync(new[] { variantA.Id, variantB.Id });

        results.Count.ShouldBe(2);
        results.ShouldContain(i => i.VariantId == variantA.Id);
        results.ShouldContain(i => i.VariantId == variantB.Id);
        results.ShouldNotContain(i => i.VariantId == variantC.Id);
    }

    [Fact]
    public async Task Update_AfterIncreaseStock_PersistsNewStockQuantity()
    {
        var variant = await PersistVariantAsync("SKU-INV-UP");
        var inventory = new InventoryBuilder()
            .WithVariantId(variant.Id)
            .WithInitialStock(10)
            .Build();
        inventory.ClearDomainEvents();

        await _sut.AddAsync(inventory);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByVariantIdWithLedgerAsync(variant.Id);
        reloaded.ShouldNotBeNull();
        var result = reloaded!.IncreaseStock(5, "restock");
        result.IsSuccess.ShouldBeTrue();
        reloaded.ClearDomainEvents();

        _sut.Update(reloaded);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        await using var freshContext = Fixture.CreateContext();
        var freshRepo = new InventoryRepository(freshContext);
        var final = await freshRepo.GetByVariantIdAsync(variant.Id);

        final.ShouldNotBeNull();
        final!.StockQuantity.Value.ShouldBe(15);
    }

    [Fact]
    public async Task AddAsync_DuplicateVariantId_ThrowsOnSaveDueToUniqueIndex()
    {
        var variant = await PersistVariantAsync("SKU-INV-DUP");

        var first = new InventoryBuilder().WithVariantId(variant.Id).WithInitialStock(5).Build();
        var second = new InventoryBuilder().WithVariantId(variant.Id).WithInitialStock(10).Build();
        first.ClearDomainEvents();
        second.ClearDomainEvents();

        await _sut.AddAsync(first);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        await _sut.AddAsync(second);

        await Should.ThrowAsync<DbUpdateException>(async () => await Context.SaveChangesAsync());
    }
}
