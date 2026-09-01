using Domain.Order.ValueObjects;
using Domain.Variant.Aggregates;
using Infrastructure.Persistence.Context;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Builders;
using Orders = Domain.Order.Aggregates.Order;
using Products = Domain.Product.Aggregates.Product;
using Users = Domain.User.Aggregates.User;

namespace Tests.Infrastructure.Order.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OrderItemConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture; private DBContext _context = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<Users> PersistUserAsync()
    {
        var user = new UserBuilder().Build();
        user.ClearDomainEvents();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return user;
    }

    private async Task<(Products product, ProductVariant variant)> PersistProductAndVariantAsync()
    {
        var category = await new CategoryBuilder().BuildAsync();
        category.ClearDomainEvents();
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var brand = await new BrandBuilder().WithCategoryId(category.Id).BuildAsync();
        brand.ClearDomainEvents();
        _context.Brands.Add(brand);
        await _context.SaveChangesAsync();

        var product = new ProductBuilder()
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        _context.Products.Add(product);

        var variant = new ProductVariantBuilder()
            .WithProductId(product.Id)
            .WithSellingPrice(100_000m)
            .Build();
        _context.ProductVariants.Add(variant);

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return (product, variant);
    }

    private async Task<Orders> BuildAndPersistOrderAsync(Users user, int quantity = 2, decimal unitPrice = 50_000m)
    {
        var (product, variant) = await PersistProductAndVariantAsync();

        var snapshot = new OrderItemSnapshotBuilder()
            .WithVariantId(variant.Id)
            .WithProductId(product.Id)
            .WithProductName(product.Name)
            .WithSku(variant.Sku)
            .WithUnitPrice(unitPrice)
            .WithQuantity(quantity)
            .Build();

        var order = new OrderBuilder()
            .WithUserId(user.Id)
            .WithIdempotencyKey(Guid.NewGuid())
            .WithItemSnapshots(snapshot)
            .Build();

        order.ClearDomainEvents();
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return order;
    }

    [Fact]
    public async Task SaveChanges_ThenReload_RoundTripsOrderItemIdConversion()
    {
        var user = await PersistUserAsync();
        var order = await BuildAndPersistOrderAsync(user);
        var originalItemId = order.OrderItems.First().Id;

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.OrderItems.FirstAsync(oi => oi.Id == originalItemId);

        loaded.Id.Value.ShouldBe(originalItemId.Value);
        loaded.OrderId.Value.ShouldBe(order.Id.Value);
    }

    [Fact]
    public async Task SaveChanges_ThenReload_PreservesProductAndVariantIds()
    {
        var user = await PersistUserAsync();
        var order = await BuildAndPersistOrderAsync(user);
        var expected = order.OrderItems.First();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.OrderItems.FirstAsync(oi => oi.Id == expected.Id);

        loaded.ProductId.Value.ShouldBe(expected.ProductId.Value);
        loaded.VariantId.Value.ShouldBe(expected.VariantId.Value);
    }

    [Fact]
    public async Task SaveChanges_ThenReload_MapsOwnedUnitPriceIntoUnitPriceAmountAndCurrencyColumns()
    {
        var user = await PersistUserAsync();
        var order = await BuildAndPersistOrderAsync(user, quantity: 1, unitPrice: 75_000m);
        var expected = order.OrderItems.First();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.OrderItems.FirstAsync(oi => oi.Id == expected.Id);

        loaded.UnitPrice.Amount.ShouldBe(75_000m);
        loaded.UnitPrice.Currency.ShouldBe("IRT");
    }

    [Fact]
    public async Task SaveChanges_ThenReload_PreservesProductNameAndSku()
    {
        var user = await PersistUserAsync();
        var order = await BuildAndPersistOrderAsync(user);
        var expected = order.OrderItems.First();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.OrderItems.FirstAsync(oi => oi.Id == expected.Id);

        loaded.ProductName.ShouldBe(expected.ProductName);
        loaded.Sku.ShouldBe(expected.Sku);
        loaded.Quantity.ShouldBe(expected.Quantity);
    }

    [Fact]
    public async Task TotalPrice_IsIgnoredByEfCore_ButComputedFromUnitPriceAndQuantityWhenLoaded()
    {
        var user = await PersistUserAsync();
        var order = await BuildAndPersistOrderAsync(user, quantity: 3, unitPrice: 40_000m);
        var expected = order.OrderItems.First();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.OrderItems.FirstAsync(oi => oi.Id == expected.Id);

        loaded.TotalPrice.Amount.ShouldBe(120_000m);
        loaded.TotalPrice.Currency.ShouldBe("IRT");
    }

    [Fact]
    public async Task DeleteOrder_CascadesToOrderItemsThroughConfiguredForeignKey()
    {
        var user = await PersistUserAsync();
        var order = await BuildAndPersistOrderAsync(user);
        var itemId = order.OrderItems.First().Id;

        await using var deletionContext = _fixture.CreateContext();
        var trackedOrder = await deletionContext.Orders
            .IgnoreQueryFilters()
            .Include(o => o.OrderItems)
            .FirstAsync(o => o.Id == order.Id);
        deletionContext.Orders.Remove(trackedOrder);
        await deletionContext.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var itemExists = await freshContext.OrderItems
            .IgnoreQueryFilters()
            .AnyAsync(oi => oi.Id == itemId);

        itemExists.ShouldBeFalse();
    }

    [Fact]
    public async Task QueryFilter_WhenParentOrderIsSoftDeleted_ExcludesChildItemsFromDefaultQuery()
    {
        var user = await PersistUserAsync();
        var order = await BuildAndPersistOrderAsync(user);
        var itemId = order.OrderItems.First().Id;

        await using var deletionContext = _fixture.CreateContext();
        var tracked = await deletionContext.Orders.FirstAsync(o => o.Id == order.Id);
        tracked.MarkAsDeleted();
        deletionContext.Orders.Update(tracked);
        await deletionContext.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var visibleItem = await freshContext.OrderItems.FirstOrDefaultAsync(oi => oi.Id == itemId);

        visibleItem.ShouldBeNull();
    }

    [Fact]
    public async Task IgnoreQueryFilters_WhenParentOrderIsSoftDeleted_ReturnsChildItemRow()
    {
        var user = await PersistUserAsync();
        var order = await BuildAndPersistOrderAsync(user);
        var itemId = order.OrderItems.First().Id;

        await using var deletionContext = _fixture.CreateContext();
        var tracked = await deletionContext.Orders.FirstAsync(o => o.Id == order.Id);
        tracked.MarkAsDeleted();
        deletionContext.Orders.Update(tracked);
        await deletionContext.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.OrderItems
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(oi => oi.Id == itemId);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(itemId);
    }

    [Fact]
    public async Task Query_ByOrderIdIndex_ReturnsAllItemsForOrder()
    {
        var user = await PersistUserAsync();
        var order = await BuildAndPersistOrderAsync(user);
        await BuildAndPersistOrderAsync(user);

        await using var freshContext = _fixture.CreateContext();
        var items = await freshContext.OrderItems
            .Where(oi => oi.OrderId == order.Id)
            .ToListAsync();

        items.Count.ShouldBe(order.OrderItems.Count);
        items.ShouldAllBe(oi => oi.OrderId == order.Id);
    }
}
