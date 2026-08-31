using Application.Inventory.Contracts;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Inventory.Aggregates;
using Domain.Product.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.ValueObjects;
using Infrastructure.Inventory.QueryServices;
using Tests.TestInfrastructure.Base;
using Inventories = Domain.Inventory.Aggregates.Inventory;
using Products = Domain.Product.Aggregates.Product;

namespace Tests.Infrastructure.Inventory.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class InventoryQueryServiceTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private IInventoryQueryService _sut = null!;
    private BrandId _brandId = null!;
    private CategoryId _categoryId = null!;

    protected override async Task OnInitializeAsync()
    {
        _sut = new InventoryQueryService(Context);

        var (brand, category) = await SeedBrandWithCategoryAsync();
        _brandId = brand.Id;
        _categoryId = category.Id;
    }

    private async Task<(Products product, ProductVariant variant, Inventories inventory)>
        SeedProductVariantAndInventoryAsync(int stock, bool isUnlimited = false, int threshold = 5, string sku = "SKU-DEFAULT")
    {
        var product = new ProductBuilder()
            .WithBrandId(_brandId)
            .WithCategoryId(_categoryId)
            .Build();
        Context.Products.Add(product);

        var variant = new ProductVariantBuilder()
            .WithProductId(product.Id)
            .WithSku(sku)
            .Build();
        Context.ProductVariants.Add(variant);

        var inventory = Inventories.Create(
            variant.Id, initialStock: stock, isUnlimited: isUnlimited, lowStockThreshold: threshold);
        Context.Inventories.Add(inventory);

        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        return (product, variant, inventory);
    }

    [Fact]
    public async Task GetByVariantIdAsync_NonExistent_ReturnsNull()
    {
        var result = await _sut.GetByVariantIdAsync(VariantId.NewId());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByVariantIdAsync_Existing_ReturnsMappedDto()
    {
        var (_, variant, _) = await SeedProductVariantAndInventoryAsync(stock: 25);

        var result = await _sut.GetByVariantIdAsync(variant.Id);

        result.ShouldNotBeNull();
        result!.VariantId.ShouldBe(variant.Id.Value);
    }

    [Fact]
    public async Task GetBatchAvailabilityAsync_EmptyInput_ReturnsEmpty()
    {
        var result = await _sut.GetBatchAvailabilityAsync(new List<VariantId>());

        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetBatchAvailabilityAsync_MixedInventories_ReturnsCorrectStates()
    {
        var (_, inStockVariant, _) = await SeedProductVariantAndInventoryAsync(stock: 20, sku: "SKU-IN");
        var (_, outVariant, _) = await SeedProductVariantAndInventoryAsync(stock: 0, sku: "SKU-OUT");
        var (_, lowVariant, _) = await SeedProductVariantAndInventoryAsync(stock: 2, threshold: 5, sku: "SKU-LOW");
        var (_, unlimitedVariant, _) = await SeedProductVariantAndInventoryAsync(stock: 0, isUnlimited: true, sku: "SKU-UNL");

        var result = await _sut.GetBatchAvailabilityAsync(new List<VariantId>
        {
            inStockVariant.Id, outVariant.Id, lowVariant.Id, unlimitedVariant.Id
        });

        result.Count.ShouldBe(4);
        var inStock = result.First(r => r.VariantId == inStockVariant.Id.Value);
        var outStock = result.First(r => r.VariantId == outVariant.Id.Value);
        var lowStock = result.First(r => r.VariantId == lowVariant.Id.Value);
        var unlimited = result.First(r => r.VariantId == unlimitedVariant.Id.Value);

        inStock.IsAvailable.ShouldBeTrue();
        outStock.IsAvailable.ShouldBeFalse();
        lowStock.IsLowStock.ShouldBeTrue();
        unlimited.IsUnlimited.ShouldBeTrue();
    }

    [Fact]
    public async Task GetTransactionsPagedAsync_NoEntries_ReturnsEmptyPagedResult()
    {
        var result = await _sut.GetTransactionsPagedAsync(
            variantId: null, transactionType: null,
            fromDate: null, toDate: null, page: 1, pageSize: 10);

        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(0);
        result.Items.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetTransactionsPagedAsync_WithEntries_FilteredByVariantId()
    {
        var (_, v1, _) = await SeedProductVariantAndInventoryAsync(stock: 100, sku: "SKU-V1");
        var (_, v2, _) = await SeedProductVariantAndInventoryAsync(stock: 100, sku: "SKU-V2");

        var entry1 = new StockLedgerEntryBuilder()
            .WithVariantId(v1.Id)
            .WithQuantity(10)
            .WithBalanceAfter(110)
            .WithReferenceNumber("REF-A")
            .BuildStockIn();
        var entry2 = new StockLedgerEntryBuilder()
            .WithVariantId(v2.Id)
            .WithQuantity(5)
            .WithBalanceAfter(105)
            .WithReferenceNumber("REF-B")
            .BuildStockIn();

        Context.StockLedgerEntries.AddRange(entry1, entry2);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var result = await _sut.GetTransactionsPagedAsync(
            variantId: v1.Id, transactionType: null,
            fromDate: null, toDate: null, page: 1, pageSize: 10);

        result.Items.ShouldAllBe(t => t.VariantId == v1.Id.Value);
    }

    [Fact]
    public async Task GetTransactionsPagedAsync_Pagination_ReturnsCorrectSubset()
    {
        var (_, variant, _) = await SeedProductVariantAndInventoryAsync(stock: 100, sku: "SKU-PG");
        for (var i = 0; i < 5; i++)
        {
            Context.StockLedgerEntries.Add(
                new StockLedgerEntryBuilder()
                    .WithVariantId(variant.Id)
                    .WithQuantity(1)
                    .WithBalanceAfter(101 + i)
                    .WithReferenceNumber($"REF-{i}")
                    .BuildStockIn());
        }
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var result = await _sut.GetTransactionsPagedAsync(
            variantId: variant.Id, transactionType: null,
            fromDate: null, toDate: null, page: 1, pageSize: 3);

        result.TotalCount.ShouldBeGreaterThanOrEqualTo(5);
        result.Items.Count.ShouldBe(3);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(3);
    }

    [Fact]
    public async Task GetLowStockProductsAsync_NoLowStock_ReturnsEmpty()
    {
        await SeedProductVariantAndInventoryAsync(stock: 100, sku: "SKU-HIGH");

        var result = await _sut.GetLowStockProductsAsync(threshold: 5);

        result.ShouldNotBeNull();
        result.Count().ShouldBe(0);
    }

    [Fact]
    public async Task GetLowStockProductsAsync_WithLowStockItems_ReturnsThem()
    {
        await SeedProductVariantAndInventoryAsync(stock: 100, sku: "SKU-HIGH");
        var (_, low1, _) = await SeedProductVariantAndInventoryAsync(stock: 2, threshold: 5, sku: "SKU-LOW1");
        var (_, low2, _) = await SeedProductVariantAndInventoryAsync(stock: 3, threshold: 5, sku: "SKU-LOW2");

        var result = await _sut.GetLowStockProductsAsync(threshold: 5);

        var list = result.ToList();
        list.Count.ShouldBe(2);
        list.Select(x => x.VariantId).ShouldContain(low1.Id.Value);
        list.Select(x => x.VariantId).ShouldContain(low2.Id.Value);
    }

    [Fact]
    public async Task GetLowStockProductsAsync_UnlimitedInventory_NotIncluded()
    {
        await SeedProductVariantAndInventoryAsync(stock: 0, isUnlimited: true, sku: "SKU-UNL");

        var result = await _sut.GetLowStockProductsAsync(threshold: 5);

        result.Count().ShouldBe(0);
    }

    [Fact]
    public async Task GetOutOfStockProductsAsync_NoOutOfStock_ReturnsEmpty()
    {
        await SeedProductVariantAndInventoryAsync(stock: 20, sku: "SKU-IN");

        var result = await _sut.GetOutOfStockProductsAsync();

        result.Count().ShouldBe(0);
    }

    [Fact]
    public async Task GetOutOfStockProductsAsync_WithOutOfStockItems_ReturnsThem()
    {
        await SeedProductVariantAndInventoryAsync(stock: 20, sku: "SKU-IN");
        var (_, out1, _) = await SeedProductVariantAndInventoryAsync(stock: 0, sku: "SKU-OUT1");
        var (_, out2, _) = await SeedProductVariantAndInventoryAsync(stock: 0, sku: "SKU-OUT2");

        var result = await _sut.GetOutOfStockProductsAsync();

        var list = result.ToList();
        list.Count.ShouldBe(2);
        list.Select(x => x.VariantId).ShouldContain(out1.Id.Value);
        list.Select(x => x.VariantId).ShouldContain(out2.Id.Value);
    }

    [Fact]
    public async Task GetOutOfStockProductsAsync_UnlimitedInventory_NotIncluded()
    {
        await SeedProductVariantAndInventoryAsync(stock: 0, isUnlimited: true, sku: "SKU-UNL");

        var result = await _sut.GetOutOfStockProductsAsync();

        result.Count().ShouldBe(0);
    }

    [Fact]
    public async Task GetStatisticsAsync_EmptyDatabase_ReturnsNull()
    {
        var result = await _sut.GetStatisticsAsync();

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetStatisticsAsync_WithMixedInventories_ReturnsCorrectCounts()
    {
        await SeedProductVariantAndInventoryAsync(stock: 50, sku: "SKU-A");
        await SeedProductVariantAndInventoryAsync(stock: 0, sku: "SKU-B");
        await SeedProductVariantAndInventoryAsync(stock: 3, threshold: 5, sku: "SKU-C");
        await SeedProductVariantAndInventoryAsync(stock: 0, isUnlimited: true, sku: "SKU-D");

        var result = await _sut.GetStatisticsAsync();

        result.ShouldNotBeNull();
        result!.TotalVariants.ShouldBe(4);
        result.InStockVariants.ShouldBe(3);
        result.OutOfStockVariants.ShouldBe(1);
        result.LowStockVariants.ShouldBe(1);
        result.UnlimitedVariants.ShouldBe(1);
    }

    [Fact]
    public async Task GetInventoryStatusAsync_NonExistent_ReturnsNull()
    {
        var result = await _sut.GetInventoryStatusAsync(VariantId.NewId());

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetInventoryStatusAsync_Existing_ReturnsMappedStatus()
    {
        var (_, variant, _) = await SeedProductVariantAndInventoryAsync(stock: 25);

        var result = await _sut.GetInventoryStatusAsync(variant.Id);

        result.ShouldNotBeNull();
        result!.VariantId.ShouldBe(variant.Id.Value);
        result.StockQuantity.ShouldBe(25);
        result.ReservedQuantity.ShouldBe(0);
        result.AvailableStock.ShouldBe(25);
        result.IsInStock.ShouldBeTrue();
        result.IsUnlimited.ShouldBeFalse();
    }

    [Fact]
    public async Task GetInventoryStatusAsync_UnlimitedInventory_ReturnsUnlimitedTrue()
    {
        var (_, variant, _) = await SeedProductVariantAndInventoryAsync(stock: 0, isUnlimited: true);

        var result = await _sut.GetInventoryStatusAsync(variant.Id);

        result.ShouldNotBeNull();
        result!.IsUnlimited.ShouldBeTrue();
        result.IsInStock.ShouldBeTrue();
    }

    [Fact]
    public async Task GetInventoryStatusesByProductAsync_NoVariantsForProduct_ReturnsEmpty()
    {
        var result = await _sut.GetInventoryStatusesByProductAsync(ProductId.NewId());

        result.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetInventoryStatusesByProductAsync_MultipleVariants_ReturnsForEachVariant()
    {
        var product = new ProductBuilder()
            .WithBrandId(_brandId)
            .WithCategoryId(_categoryId)
            .Build();
        Context.Products.Add(product);

        var v1 = new ProductVariantBuilder().WithProductId(product.Id).WithSku("SKU-X1").Build();
        var v2 = new ProductVariantBuilder().WithProductId(product.Id).WithSku("SKU-X2").Build();
        Context.ProductVariants.AddRange(v1, v2);

        var inv1 = Inventories.Create(v1.Id, initialStock: 10);
        var inv2 = Inventories.Create(v2.Id, initialStock: 0);
        Context.Inventories.AddRange(inv1, inv2);

        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var result = await _sut.GetInventoryStatusesByProductAsync(product.Id);

        result.Count.ShouldBe(2);
        result.Select(r => r.VariantId).ShouldContain(v1.Id.Value);
        result.Select(r => r.VariantId).ShouldContain(v2.Id.Value);
    }

    [Fact]
    public async Task GetWarehouseStockByVariantAsync_NonExistent_ReturnsEmpty()
    {
        var result = await _sut.GetWarehouseStockByVariantAsync(VariantId.NewId());

        result.ShouldNotBeNull();
        result.Count().ShouldBe(0);
    }

    [Fact]
    public async Task GetWarehouseStockByVariantAsync_ExistingWithNoLedger_FallsBackToDefaultWarehouse()
    {
        var (_, variant, _) = await SeedProductVariantAndInventoryAsync(stock: 30);

        var defaultWarehouse = Warehouse.Create(
            code: "MAIN",
            name: "Main Warehouse",
            city: "Tehran",
            address: null,
            phone: null,
            priority: 1,
            isDefault: true);
        Context.Warehouses.Add(defaultWarehouse);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var result = (await _sut.GetWarehouseStockByVariantAsync(variant.Id)).ToList();

        result.Count.ShouldBe(1);
        result[0].VariantId.ShouldBe(variant.Id.Value);
        result[0].Quantity.ShouldBe(30);
        result[0].WarehouseName.ShouldBe("Main Warehouse");
    }
}
