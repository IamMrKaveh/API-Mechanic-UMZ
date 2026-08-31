using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Domain.Variant.Aggregates;
using Infrastructure.Order.Repositories;
using Orders = Domain.Order.Aggregates.Order;
using Products = Domain.Product.Aggregates.Product;
using Users = Domain.User.Aggregates.User;

namespace Tests.Infrastructure.Order.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class OrderRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private IOrderRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new OrderRepository(_context);
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

        var brand = await new BrandBuilder()
            .WithCategoryId(category.Id)
            .BuildAsync();
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

    private async Task<Orders> BuildOrderForAsync(Users user, Guid? idempotencyKey = null)
    {
        var (product, variant) = await PersistProductAndVariantAsync();

        var snapshot = new OrderItemSnapshotBuilder()
            .WithVariantId(variant.Id)
            .WithProductId(product.Id)
            .WithProductName(product.Name)
            .WithSku(variant.Sku)
            .WithUnitPrice(100_000m)
            .WithQuantity(1)
            .Build();

        return new OrderBuilder()
            .WithUserId(user.Id)
            .WithIdempotencyKey(idempotencyKey ?? Guid.NewGuid())
            .WithItemSnapshots(snapshot)
            .Build();
    }

    [Fact]
    public async Task Add_ValidOrder_PersistsAcrossContexts()
    {
        var user = await PersistUserAsync();
        var order = await BuildOrderForAsync(user);
        order.ClearDomainEvents();

        _sut.Add(order);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new OrderRepository(freshContext);
        var loaded = await freshRepo.FindByIdAsync(order.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(order.Id);
        loaded.UserId.ShouldBe(user.Id);
        loaded.OrderItems.Count.ShouldBeGreaterThan(0);
        loaded.Status.ShouldBe(OrderStatusValue.Created);
    }

    [Fact]
    public async Task FindByIdAsync_WhenOrderDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.FindByIdAsync(OrderId.NewId());

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task FindByIdAsync_WhenOrderIsSoftDeleted_ReturnsNullDueToQueryFilter()
    {
        var user = await PersistUserAsync();
        var order = await BuildOrderForAsync(user);
        order.Cancel("customer request");
        order.ClearDomainEvents();

        _sut.Add(order);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var tracked = await _context.Orders.FirstAsync(o => o.Id == order.Id);
        typeof(Orders)
            .GetProperty("IsDeleted")!
            .GetSetMethod(nonPublic: true)!
            .Invoke(tracked, new object[] { true });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.FindByIdAsync(order.Id);
        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task ExistsByIdempotencyKeyAsync_WhenKeyExists_ReturnsTrue()
    {
        var user = await PersistUserAsync();
        var idempotencyKey = Guid.NewGuid();
        var order = await BuildOrderForAsync(user, idempotencyKey);
        order.ClearDomainEvents();

        _sut.Add(order);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var exists = await _sut.ExistsByIdempotencyKeyAsync(idempotencyKey);

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsByIdempotencyKeyAsync_WhenKeyDoesNotExist_ReturnsFalse()
    {
        var exists = await _sut.ExistsByIdempotencyKeyAsync(Guid.NewGuid());

        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task FindPendingExpiredAsync_ReturnsOrdersInExpirableStatusesOlderThanThirtyMinutes()
    {
        var user = await PersistUserAsync();
        var oldOrder = await BuildOrderForAsync(user);
        oldOrder.ClearDomainEvents();

        _sut.Add(oldOrder);
        await _context.SaveChangesAsync();

        var tracked = await _context.Orders.FirstAsync(o => o.Id == oldOrder.Id);
        typeof(Orders)
            .GetProperty("CreatedAt")!
            .GetSetMethod(nonPublic: true)!
            .Invoke(tracked, new object[] { DateTime.UtcNow.AddHours(-1) });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var freshOrder = await BuildOrderForAsync(user);
        freshOrder.ClearDomainEvents();
        _sut.Add(freshOrder);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var results = await _sut.FindPendingExpiredAsync();

        results.ShouldContain(o => o.Id == oldOrder.Id);
        results.ShouldNotContain(o => o.Id == freshOrder.Id);
    }

    [Fact]
    public async Task FindByOrderItemIdAsync_WhenItemExists_ReturnsParentOrder()
    {
        var user = await PersistUserAsync();
        var order = await BuildOrderForAsync(user);
        order.ClearDomainEvents();

        _sut.Add(order);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var itemId = order.OrderItems.First().Id;

        var loaded = await _sut.FindByOrderItemIdAsync(itemId);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(order.Id);
        loaded.OrderItems.ShouldContain(i => i.Id == itemId);
    }

    [Fact]
    public async Task FindByOrderItemIdAsync_WhenItemDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.FindByOrderItemIdAsync(OrderItemId.NewId());

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task Update_AfterStatusTransition_PersistsNewStatus()
    {
        var user = await PersistUserAsync();
        var order = await BuildOrderForAsync(user);
        order.MarkAsPaid(PaymentTransactionId.NewId());
        order.ClearDomainEvents();

        _sut.Add(order);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.FindByIdAsync(order.Id);
        reloaded.ShouldNotBeNull();
        reloaded!.StartProcessing();
        reloaded.ClearDomainEvents();
        _sut.Update(reloaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new OrderRepository(freshContext);
        var final = await freshRepo.FindByIdAsync(order.Id);

        final.ShouldNotBeNull();
        final!.Status.ShouldBe(OrderStatusValue.Processing);
    }

    [Fact]
    public async Task Add_DuplicateIdempotencyKey_ThrowsOnSaveDueToUniqueIndex()
    {
        var user = await PersistUserAsync();
        var idempotencyKey = Guid.NewGuid();

        var first = await BuildOrderForAsync(user, idempotencyKey);
        var second = await BuildOrderForAsync(user, idempotencyKey);
        first.ClearDomainEvents();
        second.ClearDomainEvents();

        _sut.Add(first);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        _sut.Add(second);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }
}
