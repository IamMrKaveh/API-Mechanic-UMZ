using Domain.Payment.Interfaces;
using Domain.Payment.ValueObjects;
using Infrastructure.Payment.Repositories;
using Orders = Domain.Order.Aggregates.Order;
using Users = Domain.User.Aggregates.User;

namespace Tests.Infrastructure.Payment.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class PaymentTransactionRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private IPaymentTransactionRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new PaymentRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<(Users user, Orders order)> PersistUserAndOrderAsync()
    {
        var category = await new CategoryBuilder().BuildAsync();
        category.ClearDomainEvents();
        await _context.Categories.AddAsync(category);
        await _context.SaveChangesAsync();

        var brand = await new BrandBuilder()
            .WithCategoryId(category.Id)
            .BuildAsync();
        brand.ClearDomainEvents();
        await _context.Brands.AddAsync(brand);
        await _context.SaveChangesAsync();

        var product = new ProductBuilder()
            .WithBrandId(brand.Id)
            .WithCategoryId(category.Id)
            .Build();
        await _context.Products.AddAsync(product);

        var variant = new ProductVariantBuilder()
            .WithProductId(product.Id)
            .WithSellingPrice(100_000m)
            .Build();
        await _context.ProductVariants.AddAsync(variant);

        var user = new UserBuilder().Build();
        user.ClearDomainEvents();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var snapshot = new OrderItemSnapshotBuilder()
            .WithVariantId(variant.Id)
            .WithProductId(product.Id)
            .WithProductName(product.Name)
            .WithSku(variant.Sku)
            .WithUnitPrice(100_000m)
            .WithQuantity(1)
            .Build();

        var order = new OrderBuilder()
            .WithUserId(user.Id)
            .WithItemSnapshots(snapshot)
            .Build();
        order.ClearDomainEvents();
        await _context.Orders.AddAsync(order);

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        return (user, order);
    }

    [Fact]
    public async Task AddAsync_ValidTransaction_PersistsAcrossContexts()
    {
        var (user, order) = await PersistUserAndOrderAsync();

        var transaction = new PaymentTransactionBuilder()
            .WithOrderId(order.Id)
            .WithUserId(user.Id)
            .WithAuthority("A" + Guid.NewGuid().ToString("N")[..20])
            .WithAmount(120_000m)
            .WithGateway("Zarinpal")
            .Build();
        transaction.ClearDomainEvents();

        await _sut.AddAsync(transaction);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new PaymentRepository(freshContext);
        var loaded = await freshRepo.GetByAuthorityAsync(transaction.Authority.Value);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(transaction.Id);
        loaded.OrderId.ShouldBe(order.Id);
        loaded.UserId.ShouldBe(user.Id);
        loaded.Amount.Amount.ShouldBe(120_000m);
        loaded.Status.ShouldBe(PaymentStatus.Pending);
        loaded.Gateway.Value.ShouldBe("Zarinpal");
    }

    [Fact]
    public async Task GetByAuthorityAsync_WhenAuthorityExists_ReturnsTransaction()
    {
        var (user, order) = await PersistUserAndOrderAsync();
        var authority = "AUTH-" + Guid.NewGuid().ToString("N")[..16];

        var transaction = new PaymentTransactionBuilder()
            .WithOrderId(order.Id)
            .WithUserId(user.Id)
            .WithAuthority(authority)
            .Build();
        transaction.ClearDomainEvents();

        await _sut.AddAsync(transaction);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByAuthorityAsync(authority);

        loaded.ShouldNotBeNull();
        loaded!.Authority.Value.ShouldBe(authority);
    }

    [Fact]
    public async Task GetByAuthorityAsync_WhenAuthorityDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByAuthorityAsync("MISSING-AUTHORITY-VALUE-1");

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task GetPendingExpiredTransactionsAsync_ReturnsOnlyPendingTransactionsBeforeCutoff()
    {
        var (user, order) = await PersistUserAndOrderAsync();

        var pendingExpired = new PaymentTransactionBuilder()
            .WithOrderId(order.Id)
            .WithUserId(user.Id)
            .WithAuthority("A" + Guid.NewGuid().ToString("N")[..20])
            .WithNow(DateTime.UtcNow.AddHours(-2))
            .WithExpiryMinutes(20)
            .Build();
        pendingExpired.ClearDomainEvents();

        var (user2, order2) = await PersistUserAndOrderAsync();
        var pendingFresh = new PaymentTransactionBuilder()
            .WithOrderId(order2.Id)
            .WithUserId(user2.Id)
            .WithAuthority("A" + Guid.NewGuid().ToString("N")[..20])
            .WithNow(DateTime.UtcNow)
            .WithExpiryMinutes(60)
            .Build();
        pendingFresh.ClearDomainEvents();

        await _sut.AddAsync(pendingExpired);
        await _sut.AddAsync(pendingFresh);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var cutoff = DateTime.UtcNow;
        var results = (await _sut.GetPendingExpiredTransactionsAsync(cutoff)).ToList();

        results.ShouldContain(t => t.Id == pendingExpired.Id);
        results.ShouldNotContain(t => t.Id == pendingFresh.Id);
    }

    [Fact]
    public async Task GetVerifiedByOrderIdAsync_WhenSuccessTransactionExists_ReturnsIt()
    {
        var (user, order) = await PersistUserAndOrderAsync();

        var transaction = new PaymentTransactionBuilder()
            .WithOrderId(order.Id)
            .WithUserId(user.Id)
            .WithAuthority("A" + Guid.NewGuid().ToString("N")[..20])
            .Build();
        transaction.MarkAsSuccess(refId: 987654321L, now: DateTime.UtcNow, fee: 1000m);
        transaction.ClearDomainEvents();

        await _sut.AddAsync(transaction);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetVerifiedByOrderIdAsync(order.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(transaction.Id);
        loaded.Status.ShouldBe(PaymentStatus.Success);
        loaded.RefId.ShouldBe(987654321L);
    }

    [Fact]
    public async Task GetVerifiedByOrderIdAsync_WhenOnlyPendingExists_ReturnsNull()
    {
        var (user, order) = await PersistUserAndOrderAsync();

        var transaction = new PaymentTransactionBuilder()
            .WithOrderId(order.Id)
            .WithUserId(user.Id)
            .WithAuthority("A" + Guid.NewGuid().ToString("N")[..20])
            .Build();
        transaction.ClearDomainEvents();

        await _sut.AddAsync(transaction);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetVerifiedByOrderIdAsync(order.Id);

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task GetActiveByOrderIdAsync_WhenPendingExists_ReturnsIt()
    {
        var (user, order) = await PersistUserAndOrderAsync();

        var transaction = new PaymentTransactionBuilder()
            .WithOrderId(order.Id)
            .WithUserId(user.Id)
            .WithAuthority("A" + Guid.NewGuid().ToString("N")[..20])
            .Build();
        transaction.ClearDomainEvents();

        await _sut.AddAsync(transaction);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetActiveByOrderIdAsync(order.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(transaction.Id);
        loaded.Status.ShouldBe(PaymentStatus.Pending);
    }

    [Fact]
    public async Task GetActiveByOrderIdAsync_WhenTransactionIsFailed_ReturnsNull()
    {
        var (user, order) = await PersistUserAndOrderAsync();

        var transaction = new PaymentTransactionBuilder()
            .WithOrderId(order.Id)
            .WithUserId(user.Id)
            .WithAuthority("A" + Guid.NewGuid().ToString("N")[..20])
            .Build();
        transaction.MarkAsFailed(DateTime.UtcNow, "gateway timeout");
        transaction.ClearDomainEvents();

        await _sut.AddAsync(transaction);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetActiveByOrderIdAsync(order.Id);

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task Update_AfterMarkAsSuccess_PersistsNewStatusAndRefId()
    {
        var (user, order) = await PersistUserAndOrderAsync();

        var transaction = new PaymentTransactionBuilder()
            .WithOrderId(order.Id)
            .WithUserId(user.Id)
            .WithAuthority("A" + Guid.NewGuid().ToString("N")[..20])
            .Build();
        transaction.ClearDomainEvents();

        await _sut.AddAsync(transaction);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByAuthorityAsync(transaction.Authority.Value);
        reloaded.ShouldNotBeNull();
        reloaded!.MarkAsSuccess(refId: 111222333L, now: DateTime.UtcNow, fee: 2500m);
        reloaded.ClearDomainEvents();
        _sut.Update(reloaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new PaymentRepository(freshContext);
        var final = await freshRepo.GetByAuthorityAsync(transaction.Authority.Value);

        final.ShouldNotBeNull();
        final!.Status.ShouldBe(PaymentStatus.Success);
        final.RefId.ShouldBe(111222333L);
        final.Fee.ShouldBe(2500m);
    }

    [Fact]
    public async Task AddAsync_DuplicateAuthority_ThrowsOnSaveChangesDueToUniqueIndex()
    {
        var (user, order) = await PersistUserAndOrderAsync();
        var (user2, order2) = await PersistUserAndOrderAsync();

        var authority = "A" + Guid.NewGuid().ToString("N")[..20];

        var first = new PaymentTransactionBuilder()
            .WithOrderId(order.Id)
            .WithUserId(user.Id)
            .WithAuthority(authority)
            .Build();
        first.ClearDomainEvents();

        var second = new PaymentTransactionBuilder()
            .WithOrderId(order2.Id)
            .WithUserId(user2.Id)
            .WithAuthority(authority)
            .Build();
        second.ClearDomainEvents();

        await _sut.AddAsync(first);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await _sut.AddAsync(second);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }
}
