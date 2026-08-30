using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.Payment.ValueObjects;
using Infrastructure.Order.Repositories;
using Infrastructure.Persistence.Context;
using Orders = Domain.Order.Aggregates.Order;
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

    private static Orders BuildOrderFor(Users user, Guid? idempotencyKey = null)
    {
        return new OrderBuilder()
            .WithUserId(user.Id)
            .WithIdempotencyKey(idempotencyKey ?? Guid.NewGuid())
            .Build();
    }

    [Fact]
    public async Task Add_ValidOrder_PersistsAcrossContexts()
    {
        var user = await PersistUserAsync();
        var order = BuildOrderFor(user);
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
        var order = BuildOrderFor(user);
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
        var order = BuildOrderFor(user, idempotencyKey);
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
        var oldOrder = BuildOrderFor(user);
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

        var freshOrder = BuildOrderFor(user);
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
        var order = BuildOrderFor(user);
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
        var order = BuildOrderFor(user);
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

        var first = BuildOrderFor(user, idempotencyKey);
        var second = BuildOrderFor(user, idempotencyKey);
        first.ClearDomainEvents();
        second.ClearDomainEvents();

        _sut.Add(first);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        _sut.Add(second);

        await Should.ThrowAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }
}
