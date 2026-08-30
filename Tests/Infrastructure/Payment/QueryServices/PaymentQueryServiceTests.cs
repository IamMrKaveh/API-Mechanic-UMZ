using Application.Payment.Contracts;
using Domain.Order.ValueObjects;
using Domain.Payment.Aggregates;
using Domain.User.ValueObjects;
using Infrastructure.Payment.QueryServices;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;

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

    [Fact]
    public async Task GetByAuthorityAsync_NonExistentAuthority_ReturnsNull()
    {
        var result = await _sut.GetByAuthorityAsync("A000000000000000000000");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetByAuthorityAsync_ExistingTransaction_ReturnsMappedDto()
    {
        var authority = "A" + new string('1', 24);
        var orderId = OrderId.NewId();
        var userId = UserId.NewId();
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
        var authority = "A" + new string('2', 24);
        var transaction = new PaymentTransactionBuilder()
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
        var orderId = OrderId.NewId();
        var oldTx = new PaymentTransactionBuilder()
            .WithOrderId(orderId)
            .WithAuthority("A" + new string('3', 24))
            .WithNow(new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc))
            .Build();
        var newTx = new PaymentTransactionBuilder()
            .WithOrderId(orderId)
            .WithAuthority("A" + new string('4', 24))
            .WithNow(new DateTime(2026, 5, 2, 8, 0, 0, DateTimeKind.Utc))
            .Build();
        var unrelatedTx = new PaymentTransactionBuilder()
            .WithOrderId(OrderId.NewId())
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
        var tx1 = new PaymentTransactionBuilder()
            .WithAuthority("A" + new string('6', 24))
            .WithNow(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc))
            .Build();
        var tx2 = new PaymentTransactionBuilder()
            .WithAuthority("A" + new string('7', 24))
            .WithNow(new DateTime(2026, 4, 3, 0, 0, 0, DateTimeKind.Utc))
            .Build();
        var tx3 = new PaymentTransactionBuilder()
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
        var orderId = OrderId.NewId();
        var matching = new PaymentTransactionBuilder()
            .WithOrderId(orderId)
            .WithAuthority("A" + new string('a', 24))
            .Build();
        var other = new PaymentTransactionBuilder()
            .WithOrderId(OrderId.NewId())
            .WithAuthority("A" + new string('b', 24))
            .Build();

        _context.PaymentTransactions.AddRange(matching, other);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetPagedAsync(
            orderId: orderId.Value, userId: null, status: null, gateway: null,
            fromDate: null, toDate: null, page: 1, pageSize: 10);

        result.TotalCount.ShouldBe(1);
        result.Items[0].OrderId.ShouldBe(orderId.Value);
    }

    [Fact]
    public async Task GetPagedAsync_FilteredByUserId_ReturnsMatchingOnly()
    {
        var userId = UserId.NewId();
        var matching = new PaymentTransactionBuilder()
            .WithUserId(userId)
            .WithAuthority("A" + new string('c', 24))
            .Build();
        var other = new PaymentTransactionBuilder()
            .WithUserId(UserId.NewId())
            .WithAuthority("A" + new string('d', 24))
            .Build();

        _context.PaymentTransactions.AddRange(matching, other);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetPagedAsync(
            orderId: null, userId: userId.Value, status: null, gateway: null,
            fromDate: null, toDate: null, page: 1, pageSize: 10);

        result.TotalCount.ShouldBe(1);
        result.Items[0].UserId.ShouldBe(userId.Value);
    }

    [Fact]
    public async Task GetPagedAsync_FilteredByStatusPending_ReturnsPendingOnly()
    {
        var pending = new PaymentTransactionBuilder()
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
        var zarin = new PaymentTransactionBuilder()
            .WithAuthority("A" + new string('f', 24))
            .WithGateway("Zarinpal")
            .Build();
        var mellat = new PaymentTransactionBuilder()
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
        var inside = new PaymentTransactionBuilder()
            .WithAuthority("B" + new string('1', 24))
            .WithNow(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc))
            .Build();
        var before = new PaymentTransactionBuilder()
            .WithAuthority("B" + new string('2', 24))
            .WithNow(new DateTime(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc))
            .Build();
        var after = new PaymentTransactionBuilder()
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
            var tx = new PaymentTransactionBuilder()
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
