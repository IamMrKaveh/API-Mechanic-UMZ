using Application.Payment.Contracts;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Payment.QueryServices;

namespace Tests.Infrastructure.Payment.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class PaymentQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private IPaymentQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new PaymentQueryService(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<(OrderId orderId, UserId userId)> PersistOrderAsync()
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

        var user = new UserBuilder().Build();
        user.ClearDomainEvents();
        _context.Users.Add(user);
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
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        return (order.Id, user.Id);
    }

    private async Task<PaymentTransactionBuilder> NewTransactionBuilderAsync(
        OrderId? orderId = null,
        UserId? userId = null)
    {
        if (orderId is null || userId is null)
        {
            var (persistedOrderId, persistedUserId) = await PersistOrderAsync();
            orderId ??= persistedOrderId;
            userId ??= persistedUserId;
        }

        return new PaymentTransactionBuilder()
            .WithOrderId(orderId)
            .WithUserId(userId);
    }

    [Fact]
    public async Task GetByAuthorityAsync_NonExistentAuthority_ReturnsNull()
    {
        var result = await _sut.GetByAuthorityAsync("A000000000000000000000");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByAuthorityAsync_ExistingTransaction_ReturnsMappedDto()
    {
        var (orderId, userId) = await PersistOrderAsync();
        var authority = "A" + new string('1', 24);
        var transaction = new PaymentTransactionBuilder()
            .WithOrderId(orderId)
            .WithUserId(userId)
            .WithAuthority(authority)
            .WithAmount(150_000m)
            .WithGateway("Zarinpal")
            .WithNow(new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc))
            .Build();

        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByAuthorityAsync(authority);

        result.ShouldNotBeNull();
        result!.Authority.ShouldBe(authority);
        result.OrderId.ShouldBe(orderId.Value);
        result.UserId.ShouldBe(userId.Value);
        result.Amount.ShouldBe(150_000m);
        result.Gateway.ShouldBe("Zarinpal");
        result.Status.ShouldBe("Pending");
    }

    [Fact]
    public async Task GetStatusByAuthorityAsync_NonExistentAuthority_ReturnsNull()
    {
        var result = await _sut.GetStatusByAuthorityAsync("A" + new string('9', 24));

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetStatusByAuthorityAsync_PendingTransaction_ReturnsStatusWithIsSuccessFalse()
    {
        var (orderId, userId) = await PersistOrderAsync();
        var authority = "A" + new string('2', 24);
        var transaction = new PaymentTransactionBuilder()
            .WithOrderId(orderId)
            .WithUserId(userId)
            .WithAuthority(authority)
            .WithAmount(250_000m)
            .Build();

        _context.PaymentTransactions.Add(transaction);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetStatusByAuthorityAsync(authority);

        result.ShouldNotBeNull();
        result!.Authority.ShouldBe(authority);
        result.Status.ShouldBe("Pending");
        result.IsSuccess.ShouldBeFalse();
        result.Amount.ShouldBe(250_000m);
        result.RefId.ShouldBeNull();
    }

    [Fact]
    public async Task GetByOrderIdAsync_NoTransactionsForOrder_ReturnsEmptyList()
    {
        var result = await _sut.GetByOrderIdAsync(OrderId.NewId());

        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetByOrderIdAsync_MultipleTransactions_ReturnsOrderedByCreatedAtDescending()
    {
        var (orderId, userId) = await PersistOrderAsync();
        var (unrelatedOrderId, unrelatedUserId) = await PersistOrderAsync();

        var oldTx = new PaymentTransactionBuilder()
            .WithOrderId(orderId)
            .WithUserId(userId)
            .WithAuthority("A" + new string('3', 24))
            .WithNow(new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc))
            .Build();
        var newTx = new PaymentTransactionBuilder()
            .WithOrderId(orderId)
            .WithUserId(userId)
            .WithAuthority("A" + new string('4', 24))
            .WithNow(new DateTime(2026, 5, 2, 8, 0, 0, DateTimeKind.Utc))
            .Build();
        var unrelatedTx = new PaymentTransactionBuilder()
            .WithOrderId(unrelatedOrderId)
            .WithUserId(unrelatedUserId)
            .WithAuthority("A" + new string('5', 24))
            .Build();

        _context.PaymentTransactions.AddRange(oldTx, newTx, unrelatedTx);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByOrderIdAsync(orderId);

        result.Count.ShouldBe(2);
        result[0].CreatedAt.ShouldBeGreaterThan(result[1].CreatedAt);
    }

    [Fact]
    public async Task GetPagedAsync_NoFilters_ReturnsAllPagedByCreatedAtDescending()
    {
        var (o1, u1) = await PersistOrderAsync();
        var (o2, u2) = await PersistOrderAsync();
        var (o3, u3) = await PersistOrderAsync();

        var tx1 = new PaymentTransactionBuilder()
            .WithOrderId(o1).WithUserId(u1)
            .WithAuthority("A" + new string('6', 24))
            .WithNow(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc))
            .Build();
        var tx2 = new PaymentTransactionBuilder()
            .WithOrderId(o2).WithUserId(u2)
            .WithAuthority("A" + new string('7', 24))
            .WithNow(new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc))
            .Build();
        var tx3 = new PaymentTransactionBuilder()
            .WithOrderId(o3).WithUserId(u3)
            .WithAuthority("A" + new string('8', 24))
            .WithNow(new DateTime(2026, 4, 2, 0, 0, 0, DateTimeKind.Utc))
            .Build();

        _context.PaymentTransactions.AddRange(tx1, tx2, tx3);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetPagedAsync(
            orderId: null, userId: null, status: null, gateway: null,
            fromDate: null, toDate: null, page: 1, pageSize: 10);

        result.TotalCount.ShouldBe(3);
        result.Items.Count.ShouldBe(3);
        result.Items[0].CreatedAt.ShouldBeGreaterThan(result.Items[1].CreatedAt);
        result.Items[1].CreatedAt.ShouldBeGreaterThan(result.Items[2].CreatedAt);
    }

    [Fact]
    public async Task GetPagedAsync_FilteredByOrderId_ReturnsMatchingOnly()
    {
        var (targetOrderId, targetUserId) = await PersistOrderAsync();
        var (otherOrderId, otherUserId) = await PersistOrderAsync();

        var matching = new PaymentTransactionBuilder()
            .WithOrderId(targetOrderId).WithUserId(targetUserId)
            .WithAuthority("A" + new string('a', 24))
            .Build();
        var other = new PaymentTransactionBuilder()
            .WithOrderId(otherOrderId).WithUserId(otherUserId)
            .WithAuthority("A" + new string('b', 24))
            .Build();

        _context.PaymentTransactions.AddRange(matching, other);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetPagedAsync(
            orderId: targetOrderId.Value, userId: null, status: null, gateway: null,
            fromDate: null, toDate: null, page: 1, pageSize: 10);

        result.TotalCount.ShouldBe(1);
        result.Items[0].OrderId.ShouldBe(targetOrderId.Value);
    }

    [Fact]
    public async Task GetPagedAsync_FilteredByUserId_ReturnsMatchingOnly()
    {
        var (o1, targetUserId) = await PersistOrderAsync();
        var (o2, otherUserId) = await PersistOrderAsync();

        var matching = new PaymentTransactionBuilder()
            .WithOrderId(o1).WithUserId(targetUserId)
            .WithAuthority("A" + new string('c', 24))
            .Build();
        var other = new PaymentTransactionBuilder()
            .WithOrderId(o2).WithUserId(otherUserId)
            .WithAuthority("A" + new string('d', 24))
            .Build();

        _context.PaymentTransactions.AddRange(matching, other);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetPagedAsync(
            orderId: null, userId: targetUserId.Value, status: null, gateway: null,
            fromDate: null, toDate: null, page: 1, pageSize: 10);

        result.TotalCount.ShouldBe(1);
        result.Items[0].UserId.ShouldBe(targetUserId.Value);
    }

    [Fact]
    public async Task GetPagedAsync_FilteredByStatusPending_ReturnsPendingOnly()
    {
        var (orderId, userId) = await PersistOrderAsync();
        var pending = new PaymentTransactionBuilder()
            .WithOrderId(orderId).WithUserId(userId)
            .WithAuthority("A" + new string('e', 24))
            .Build();

        _context.PaymentTransactions.Add(pending);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var pendingResult = await _sut.GetPagedAsync(
            orderId: null, userId: null, status: "Pending", gateway: null,
            fromDate: null, toDate: null, page: 1, pageSize: 10);
        var successResult = await _sut.GetPagedAsync(
            orderId: null, userId: null, status: "Success", gateway: null,
            fromDate: null, toDate: null, page: 1, pageSize: 10);

        pendingResult.TotalCount.ShouldBe(1);
        successResult.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task GetPagedAsync_FilteredByGateway_ReturnsMatchingOnly()
    {
        var (o1, u1) = await PersistOrderAsync();
        var (o2, u2) = await PersistOrderAsync();

        var zarin = new PaymentTransactionBuilder()
            .WithOrderId(o1).WithUserId(u1)
            .WithAuthority("A" + new string('f', 24))
            .WithGateway("Zarinpal")
            .Build();
        var mellat = new PaymentTransactionBuilder()
            .WithOrderId(o2).WithUserId(u2)
            .WithAuthority("B" + new string('0', 24))
            .WithGateway("Mellat")
            .Build();

        _context.PaymentTransactions.AddRange(zarin, mellat);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetPagedAsync(
            orderId: null, userId: null, status: null, gateway: "Zarinpal",
            fromDate: null, toDate: null, page: 1, pageSize: 10);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Gateway.ShouldBe("Zarinpal");
    }

    [Fact]
    public async Task GetPagedAsync_FilteredByDateRange_ReturnsOnlyWithinRange()
    {
        var (o1, u1) = await PersistOrderAsync();
        var (o2, u2) = await PersistOrderAsync();
        var (o3, u3) = await PersistOrderAsync();

        var inside = new PaymentTransactionBuilder()
            .WithOrderId(o1).WithUserId(u1)
            .WithAuthority("B" + new string('1', 24))
            .WithNow(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc))
            .Build();
        var before = new PaymentTransactionBuilder()
            .WithOrderId(o2).WithUserId(u2)
            .WithAuthority("B" + new string('2', 24))
            .WithNow(new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc))
            .Build();
        var after = new PaymentTransactionBuilder()
            .WithOrderId(o3).WithUserId(u3)
            .WithAuthority("B" + new string('3', 24))
            .WithNow(new DateTime(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc))
            .Build();

        _context.PaymentTransactions.AddRange(inside, before, after);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetPagedAsync(
            orderId: null, userId: null, status: null, gateway: null,
            fromDate: new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc),
            toDate: new DateTime(2026, 6, 20, 0, 0, 0, DateTimeKind.Utc),
            page: 1, pageSize: 10);

        result.TotalCount.ShouldBe(1);
        result.Items[0].Authority.ShouldBe("B" + new string('1', 24));
    }

    [Theory]
    [InlineData(1, 2, 2, 3)]
    [InlineData(2, 2, 1, 3)]
    [InlineData(1, 10, 3, 3)]
    public async Task GetPagedAsync_Pagination_ReturnsExpectedPageAndTotal(
        int page, int pageSize, int expectedItems, int expectedTotal)
    {
        for (var i = 0; i < 3; i++)
        {
            var (orderId, userId) = await PersistOrderAsync();
            var tx = new PaymentTransactionBuilder()
                .WithOrderId(orderId).WithUserId(userId)
                .WithAuthority($"C{i}" + new string('0', 23))
                .WithNow(new DateTime(2026, 7, i + 1, 0, 0, 0, DateTimeKind.Utc))
                .Build();
            _context.PaymentTransactions.Add(tx);
        }
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetPagedAsync(
            orderId: null, userId: null, status: null, gateway: null,
            fromDate: null, toDate: null, page: page, pageSize: pageSize);

        result.TotalCount.ShouldBe(expectedTotal);
        result.Items.Count.ShouldBe(expectedItems);
        result.Page.ShouldBe(page);
        result.PageSize.ShouldBe(pageSize);
    }
}
