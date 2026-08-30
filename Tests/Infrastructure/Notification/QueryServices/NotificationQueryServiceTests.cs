using Application.Notification.Contracts;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Notification.QueryServices;
using Infrastructure.Persistence.Context;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Notification.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class NotificationQueryServiceTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;
    private INotificationQueryService _sut = null!;

    public Task InitializeAsync()
    {
        Skip.IfNot(_fixture.IsDockerAvailable, _fixture.UnavailabilityReason ?? "Docker engine not available.");

        _context = _fixture.CreateContext();
        _sut = new NotificationQueryService(_context);
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
    public async Task GetByUserIdAsync_NoNotifications_ReturnsEmptyPagedResult()
    {
        var result = await _sut.GetByUserIdAsync(UserId.NewId(), 1, 10);

        result.ShouldNotBeNull();
        result.TotalCount.ShouldBe(0);
        result.Items.Count.ShouldBe(0);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(10);
    }

    [Fact]
    public async Task GetByUserIdAsync_UserWithNotifications_ReturnsOnlyForThatUser()
    {
        var targetUserId = UserId.NewId();
        var otherUserId = UserId.NewId();

        var forTarget1 = new NotificationBuilder()
            .WithUserId(targetUserId)
            .WithTitle("Target Order")
            .WithMessage("Your order was placed")
            .Build();
        var forTarget2 = new NotificationBuilder()
            .WithUserId(targetUserId)
            .WithTitle("Target Shipping")
            .WithMessage("Your order was shipped")
            .WithType(NotificationType.OrderShipped)
            .Build();
        var forOther = new NotificationBuilder()
            .WithUserId(otherUserId)
            .WithTitle("Other")
            .WithMessage("Other user message")
            .Build();

        _context.Notifications.AddRange(forTarget1, forTarget2, forOther);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByUserIdAsync(targetUserId, 1, 10);

        result.TotalCount.ShouldBe(2);
        result.Items.Count.ShouldBe(2);
        result.Items.ShouldAllBe(n => n.UserId == targetUserId.Value);
    }

    [Fact]
    public async Task GetByUserIdAsync_MappedFields_MatchAggregate()
    {
        var userId = UserId.NewId();
        var notification = new NotificationBuilder()
            .WithUserId(userId)
            .WithTitle("Custom Title")
            .WithMessage("Custom Message")
            .WithType(NotificationType.OrderPaid)
            .WithActionUrl("/orders/123")
            .Build();

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByUserIdAsync(userId, 1, 10);

        result.Items.Count.ShouldBe(1);
        var dto = result.Items[0];
        dto.UserId.ShouldBe(userId.Value);
        dto.Title.ShouldBe("Custom Title");
        dto.Message.ShouldBe("Custom Message");
        dto.Type.ShouldBe("OrderPaid");
        dto.ActionUrl.ShouldBe("/orders/123");
        dto.IsRead.ShouldBeFalse();
        dto.Id.ShouldBe(notification.Id.Value);
    }

    [Fact]
    public async Task GetByUserIdAsync_MultipleNotifications_ReturnsOrderedByCreatedAtDescending()
    {
        var userId = UserId.NewId();
        var first = new NotificationBuilder().WithUserId(userId).WithTitle("First").Build();
        await Task.Delay(20);
        var second = new NotificationBuilder().WithUserId(userId).WithTitle("Second").Build();
        await Task.Delay(20);
        var third = new NotificationBuilder().WithUserId(userId).WithTitle("Third").Build();

        _context.Notifications.AddRange(first, second, third);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByUserIdAsync(userId, 1, 10);

        result.Items.Count.ShouldBe(3);
        result.Items[0].CreatedAt.ShouldBeGreaterThanOrEqualTo(result.Items[1].CreatedAt);
        result.Items[1].CreatedAt.ShouldBeGreaterThanOrEqualTo(result.Items[2].CreatedAt);
    }

    [Theory]
    [InlineData(1, 2, 2, 4)]
    [InlineData(2, 2, 2, 4)]
    [InlineData(3, 2, 0, 4)]
    public async Task GetByUserIdAsync_Pagination_ReturnsExpectedPageAndTotal(
        int page, int pageSize, int expectedItems, int expectedTotal)
    {
        var userId = UserId.NewId();
        for (var i = 0; i < 4; i++)
        {
            _context.Notifications.Add(
                new NotificationBuilder()
                    .WithUserId(userId)
                    .WithTitle($"Title {i}")
                    .Build());
            await Task.Delay(10);
        }
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetByUserIdAsync(userId, page, pageSize);

        result.TotalCount.ShouldBe(expectedTotal);
        result.Items.Count.ShouldBe(expectedItems);
        result.Page.ShouldBe(page);
        result.PageSize.ShouldBe(pageSize);
    }

    [Fact]
    public async Task GetUnreadCountAsync_NoNotifications_ReturnsZero()
    {
        var result = await _sut.GetUnreadCountAsync(UserId.NewId());

        result.ShouldBe(0);
    }

    [Fact]
    public async Task GetUnreadCountAsync_MixOfReadAndUnread_CountsOnlyUnread()
    {
        var userId = UserId.NewId();
        var read1 = new NotificationBuilder().WithUserId(userId).WithTitle("Read1").Build();
        read1.MarkAsRead();
        var read2 = new NotificationBuilder().WithUserId(userId).WithTitle("Read2").Build();
        read2.MarkAsRead();
        var unread1 = new NotificationBuilder().WithUserId(userId).WithTitle("Unread1").Build();
        var unread2 = new NotificationBuilder().WithUserId(userId).WithTitle("Unread2").Build();
        var unread3 = new NotificationBuilder().WithUserId(userId).WithTitle("Unread3").Build();

        _context.Notifications.AddRange(read1, read2, unread1, unread2, unread3);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetUnreadCountAsync(userId);

        result.ShouldBe(3);
    }

    [Fact]
    public async Task GetUnreadCountAsync_OtherUsersUnread_NotCounted()
    {
        var userId = UserId.NewId();
        var otherUserId = UserId.NewId();
        _context.Notifications.Add(new NotificationBuilder().WithUserId(userId).Build());
        _context.Notifications.Add(new NotificationBuilder().WithUserId(otherUserId).Build());
        _context.Notifications.Add(new NotificationBuilder().WithUserId(otherUserId).Build());
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetUnreadCountAsync(userId);

        result.ShouldBe(1);
    }

    [Fact]
    public async Task GetAllAsync_NoNotifications_ReturnsEmptyPagedResult()
    {
        var result = await _sut.GetAllAsync(1, 10);

        result.TotalCount.ShouldBe(0);
        result.Items.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetAllAsync_MultipleUsers_ReturnsAllNotifications()
    {
        var user1 = UserId.NewId();
        var user2 = UserId.NewId();
        _context.Notifications.Add(new NotificationBuilder().WithUserId(user1).Build());
        _context.Notifications.Add(new NotificationBuilder().WithUserId(user2).Build());
        _context.Notifications.Add(new NotificationBuilder().WithUserId(user2).Build());
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetAllAsync(1, 10);

        result.TotalCount.ShouldBe(3);
        result.Items.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetAllAsync_Ordering_ReturnsNewestFirst()
    {
        var user = UserId.NewId();
        var older = new NotificationBuilder().WithUserId(user).WithTitle("older").Build();
        await Task.Delay(20);
        var newer = new NotificationBuilder().WithUserId(user).WithTitle("newer").Build();

        _context.Notifications.AddRange(older, newer);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _sut.GetAllAsync(1, 10);

        result.Items[0].Title.ShouldBe("newer");
        result.Items[1].Title.ShouldBe("older");
    }
}
