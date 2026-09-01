using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
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
public class OrderConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
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

    private async Task<Orders> BuildOrderForAsync(
        Users user,
        Guid? idempotencyKey = null,
        Money? shippingCost = null,
        Money? discountAmount = null)
    {
        var (product, variant) = await PersistProductAndVariantAsync();

        var snapshot = new OrderItemSnapshotBuilder()
            .WithVariantId(variant.Id)
            .WithProductId(product.Id)
            .WithProductName(product.Name)
            .WithSku(variant.Sku)
            .WithUnitPrice(100_000m)
            .WithQuantity(2)
            .Build();

        return new OrderBuilder()
            .WithUserId(user.Id)
            .WithIdempotencyKey(idempotencyKey ?? Guid.NewGuid())
            .WithShippingCost(shippingCost ?? Money.Create(15_000m, "IRT"))
            .WithDiscountAmount(discountAmount ?? Money.Create(5_000m, "IRT"))
            .WithItemSnapshots(snapshot)
            .Build();
    }

    private async Task PersistAsync(Orders order)
    {
        order.ClearDomainEvents();
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task SaveChanges_ThenReload_RoundTripsOrderIdAndUserIdConversions()
    {
        var user = await PersistUserAsync();
        var order = await BuildOrderForAsync(user);
        await PersistAsync(order);

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.Orders.FirstAsync(o => o.Id == order.Id);

        loaded.Id.Value.ShouldBe(order.Id.Value);
        loaded.UserId.Value.ShouldBe(user.Id.Value);
    }

    [Fact]
    public async Task SaveChanges_ThenReload_PreservesOrderNumberAndStatusValueConversions()
    {
        var user = await PersistUserAsync();
        var order = await BuildOrderForAsync(user);
        await PersistAsync(order);

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.Orders.FirstAsync(o => o.Id == order.Id);

        loaded.OrderNumber.Value.ShouldBe(order.OrderNumber.Value);
        loaded.Status.ShouldBe(OrderStatusValue.Created);
    }

    [Fact]
    public async Task SaveChanges_ThenReload_PersistsOwnedReceiverInfoIntoConfiguredColumns()
    {
        var user = await PersistUserAsync();
        var receiver = ReceiverInfo.Create("علی محمدی", "09121234567");
        var order = await BuildOrderForAsync(user);
        typeof(Orders)
            .GetProperty(nameof(Orders.ReceiverInfo))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(order, new object[] { receiver });

        await PersistAsync(order);

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.Orders.FirstAsync(o => o.Id == order.Id);

        loaded.ReceiverInfo.FullName.ShouldBe("علی محمدی");
        loaded.ReceiverInfo.PhoneNumber.ShouldBe("09121234567");
    }

    [Fact]
    public async Task SaveChanges_ThenReload_PersistsOwnedDeliveryAddressIntoConfiguredColumns()
    {
        var user = await PersistUserAsync();
        var address = DeliveryAddress.Create("تهران", "تهران", "خیابان آزادی", "1234567890");
        var order = await BuildOrderForAsync(user);
        typeof(Orders)
            .GetProperty(nameof(Orders.DeliveryAddress))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(order, new object[] { address });

        await PersistAsync(order);

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.Orders.FirstAsync(o => o.Id == order.Id);

        loaded.DeliveryAddress.Province.ShouldBe("تهران");
        loaded.DeliveryAddress.City.ShouldBe("تهران");
        loaded.DeliveryAddress.Street.ShouldBe("خیابان آزادی");
        loaded.DeliveryAddress.PostalCode.ShouldBe("1234567890");
    }

    [Fact]
    public async Task SaveChanges_ThenReload_PersistsAllOwnedMoneyValuesWithCurrency()
    {
        var user = await PersistUserAsync();
        var order = await BuildOrderForAsync(
            user,
            shippingCost: Money.Create(25_000m, "IRT"),
            discountAmount: Money.Create(10_000m, "IRT"));
        await PersistAsync(order);

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.Orders.FirstAsync(o => o.Id == order.Id);

        loaded.ShippingCost.Amount.ShouldBe(25_000m);
        loaded.ShippingCost.Currency.ShouldBe("IRT");
        loaded.DiscountAmount.Amount.ShouldBe(10_000m);
        loaded.DiscountAmount.Currency.ShouldBe("IRT");
        loaded.SubTotal.Amount.ShouldBe(200_000m);
        loaded.SubTotal.Currency.ShouldBe("IRT");
        loaded.FinalAmount.Amount.ShouldBe(215_000m);
        loaded.FinalAmount.Currency.ShouldBe("IRT");
    }

    [Fact]
    public async Task SaveChanges_DuplicateIdempotencyKey_ThrowsDbUpdateException()
    {
        var user = await PersistUserAsync();
        var idempotencyKey = Guid.NewGuid();

        var first = await BuildOrderForAsync(user, idempotencyKey);
        await PersistAsync(first);

        var second = await BuildOrderForAsync(user, idempotencyKey);
        second.ClearDomainEvents();

        await Should.ThrowAsync<DbUpdateException>(async () =>
        {
            await _context.Orders.AddAsync(second);
            await _context.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task SaveChanges_DuplicateOrderNumber_ThrowsDbUpdateException()
    {
        var user = await PersistUserAsync();

        var first = await BuildOrderForAsync(user);
        await PersistAsync(first);

        var second = await BuildOrderForAsync(user);
        typeof(Orders)
            .GetProperty(nameof(Orders.OrderNumber))!
            .GetSetMethod(nonPublic: true)!
            .Invoke(second, new object[] { first.OrderNumber });
        second.ClearDomainEvents();

        await Should.ThrowAsync<DbUpdateException>(async () =>
        {
            await _context.Orders.AddAsync(second);
            await _context.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task QueryFilter_WithSoftDeletedOrder_ExcludesFromDefaultQuery()
    {
        var user = await PersistUserAsync();
        var order = await BuildOrderForAsync(user);
        await PersistAsync(order);

        await using var deletionContext = _fixture.CreateContext();
        var tracked = await deletionContext.Orders.FirstAsync(o => o.Id == order.Id);
        tracked.MarkAsDeleted();
        deletionContext.Orders.Update(tracked);
        await deletionContext.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var visible = await freshContext.Orders.FirstOrDefaultAsync(o => o.Id == order.Id);

        visible.ShouldBeNull();
    }

    [Fact]
    public async Task IgnoreQueryFilters_WithSoftDeletedOrder_ReturnsSoftDeletedRow()
    {
        var user = await PersistUserAsync();
        var order = await BuildOrderForAsync(user);
        await PersistAsync(order);

        await using var deletionContext = _fixture.CreateContext();
        var tracked = await deletionContext.Orders.FirstAsync(o => o.Id == order.Id);
        tracked.MarkAsDeleted();
        deletionContext.Orders.Update(tracked);
        await deletionContext.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.Orders
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == order.Id);

        loaded.ShouldNotBeNull();
        loaded!.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveChanges_ThenUpdate_XminConcurrencyTokenChangesBetweenInsertAndUpdate()
    {
        var user = await PersistUserAsync();
        var order = await BuildOrderForAsync(user);
        await PersistAsync(order);

        await using var initialContext = _fixture.CreateContext();
        var trackedInitial = await initialContext.Orders.FirstAsync(o => o.Id == order.Id);
        var initialXmin = initialContext.Entry(trackedInitial).Property<uint>("xmin").CurrentValue;

        await using var mutationContext = _fixture.CreateContext();
        var trackedForUpdate = await mutationContext.Orders.FirstAsync(o => o.Id == order.Id);
        trackedForUpdate.MoveToPending();
        trackedForUpdate.ClearDomainEvents();
        mutationContext.Orders.Update(trackedForUpdate);
        await mutationContext.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var trackedFinal = await freshContext.Orders.FirstAsync(o => o.Id == order.Id);
        var updatedXmin = freshContext.Entry(trackedFinal).Property<uint>("xmin").CurrentValue;

        updatedXmin.ShouldNotBe(initialXmin);
    }

    [Fact]
    public async Task Query_ByUserIdIndex_ReturnsMatchingOrders()
    {
        var user = await PersistUserAsync();
        var another = await PersistUserAsync();

        var mine = await BuildOrderForAsync(user);
        var other = await BuildOrderForAsync(another);
        await PersistAsync(mine);
        await PersistAsync(other);

        await using var freshContext = _fixture.CreateContext();
        var result = await freshContext.Orders
            .Where(o => o.UserId == user.Id)
            .ToListAsync();

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe(mine.Id);
    }

    [Fact]
    public async Task Update_AfterAssignPaymentMethod_PersistsPaymentMethodIdForeignKey()
    {
        var user = await PersistUserAsync();
        var order = await BuildOrderForAsync(user);
        await PersistAsync(order);

        var paymentMethod = new PaymentMethodBuilder()
            .WithName($"pm-{Guid.NewGuid():N}")
            .WithCode($"code-{Guid.NewGuid():N}"[..20])
            .Build();
        paymentMethod.ClearDomainEvents();
        await _context.PaymentMethods.AddAsync(paymentMethod);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await using var mutationContext = _fixture.CreateContext();
        var tracked = await mutationContext.Orders.FirstAsync(o => o.Id == order.Id);
        tracked.AssignPaymentMethod(paymentMethod.Id);
        mutationContext.Orders.Update(tracked);
        await mutationContext.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var loaded = await freshContext.Orders.FirstAsync(o => o.Id == order.Id);

        loaded.PaymentMethodId.ShouldNotBeNull();
        loaded.PaymentMethodId!.Value.ShouldBe(paymentMethod.Id.Value);
    }
}
