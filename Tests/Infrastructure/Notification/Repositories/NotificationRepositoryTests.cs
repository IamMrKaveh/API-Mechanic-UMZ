using Domain.Notification.Interfaces;
using Domain.Notification.ValueObjects;
using Infrastructure.Notification.Repositories;
using Infrastructure.Persistence.Context;
using Users = Domain.User.Aggregates.User;

namespace Tests.Infrastructure.Notification.Repositories;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class NotificationRepositoryTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private INotificationRepository _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new NotificationRepository(_context);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (!_fixture.IsDockerAvailable)
            return;

        await _context.DisposeAsync();
        await _fixture.ResetAsync();
    }

    private async Task<Users> PersistActiveUserAsync()
    {
        var user = new UserBuilder().Build();
        user.ClearDomainEvents();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return user;
    }

    [Fact]
    public async Task AddAsync_ValidNotification_PersistsAcrossContexts()
    {
        var user = await PersistActiveUserAsync();

        var notification = new NotificationBuilder()
            .WithUserId(user.Id)
            .WithType(NotificationType.OrderCreated)
            .WithTitle("سفارش شما ثبت شد")
            .WithMessage("سفارش شما با موفقیت ثبت شد.")
            .WithActionUrl("/orders/123")
            .Build();
        notification.ClearDomainEvents();

        await _sut.AddAsync(notification);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new NotificationRepository(freshContext);
        var loaded = await freshRepo.GetByIdAsync(notification.Id);

        loaded.ShouldNotBeNull();
        loaded!.Id.ShouldBe(notification.Id);
        loaded.UserId.ShouldBe(user.Id);
        loaded.Title.ShouldBe("سفارش شما ثبت شد");
        loaded.Message.ShouldBe("سفارش شما با موفقیت ثبت شد.");
        loaded.ActionUrl.ShouldBe("/orders/123");
        loaded.Type.Value.ShouldBe(NotificationType.OrderCreated.Value);
        loaded.IsRead.ShouldBeFalse();
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotificationDoesNotExist_ReturnsNull()
    {
        var loaded = await _sut.GetByIdAsync(NotificationId.NewId());

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenOwnerIsDeactivated_ReturnsNullDueToQueryFilter()
    {
        var user = await PersistActiveUserAsync();

        var notification = new NotificationBuilder()
            .WithUserId(user.Id)
            .Build();
        notification.ClearDomainEvents();

        await _sut.AddAsync(notification);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var trackedUser = await _context.Users.FirstAsync(u => u.Id == user.Id);
        trackedUser.Deactivate();
        trackedUser.ClearDomainEvents();
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(notification.Id);

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task GetUnreadByUserIdAsync_ReturnsOnlyUnreadNotificationsForThatUser()
    {
        var userA = await PersistActiveUserAsync();
        var userB = await PersistActiveUserAsync();

        var unreadA1 = new NotificationBuilder().WithUserId(userA.Id).WithTitle("A1").Build();
        var unreadA2 = new NotificationBuilder().WithUserId(userA.Id).WithTitle("A2").Build();
        var readA = new NotificationBuilder().WithUserId(userA.Id).WithTitle("A-read").Build();
        readA.MarkAsRead();
        var unreadB = new NotificationBuilder().WithUserId(userB.Id).WithTitle("B1").Build();

        unreadA1.ClearDomainEvents();
        unreadA2.ClearDomainEvents();
        readA.ClearDomainEvents();
        unreadB.ClearDomainEvents();

        await _sut.AddAsync(unreadA1);
        await _sut.AddAsync(unreadA2);
        await _sut.AddAsync(readA);
        await _sut.AddAsync(unreadB);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var results = await _sut.GetUnreadByUserIdAsync(userA.Id);

        results.Count.ShouldBe(2);
        results.ShouldContain(n => n.Id == unreadA1.Id);
        results.ShouldContain(n => n.Id == unreadA2.Id);
        results.ShouldNotContain(n => n.Id == readA.Id);
        results.ShouldNotContain(n => n.Id == unreadB.Id);
    }

    [Fact]
    public async Task GetUnreadByUserIdAsync_ReturnsResultsOrderedByCreatedAtDescending()
    {
        var user = await PersistActiveUserAsync();

        var first = new NotificationBuilder().WithUserId(user.Id).WithTitle("first").Build();
        first.ClearDomainEvents();
        await _sut.AddAsync(first);
        await _context.SaveChangesAsync();

        await Task.Delay(20);

        var second = new NotificationBuilder().WithUserId(user.Id).WithTitle("second").Build();
        second.ClearDomainEvents();
        await _sut.AddAsync(second);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var results = await _sut.GetUnreadByUserIdAsync(user.Id);

        results.Count.ShouldBe(2);
        results[0].CreatedAt.ShouldBeGreaterThanOrEqualTo(results[1].CreatedAt);
    }

    [Fact]
    public async Task Update_AfterMarkAsRead_PersistsIsReadTrue()
    {
        var user = await PersistActiveUserAsync();

        var notification = new NotificationBuilder().WithUserId(user.Id).Build();
        notification.ClearDomainEvents();

        await _sut.AddAsync(notification);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _sut.GetByIdAsync(notification.Id);
        reloaded.ShouldNotBeNull();
        reloaded!.MarkAsRead();
        reloaded.ClearDomainEvents();
        _sut.Update(reloaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await using var freshContext = _fixture.CreateContext();
        var freshRepo = new NotificationRepository(freshContext);
        var final = await freshRepo.GetByIdAsync(notification.Id);

        final.ShouldNotBeNull();
        final!.IsRead.ShouldBeTrue();
        final.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Remove_ExistingNotification_DeletesFromDatabase()
    {
        var user = await PersistActiveUserAsync();

        var notification = new NotificationBuilder().WithUserId(user.Id).Build();
        notification.ClearDomainEvents();

        await _sut.AddAsync(notification);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var toRemove = await _sut.GetByIdAsync(notification.Id);
        toRemove.ShouldNotBeNull();
        _sut.Remove(toRemove!);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var loaded = await _sut.GetByIdAsync(notification.Id);
        loaded.ShouldBeNull();
    }
}
