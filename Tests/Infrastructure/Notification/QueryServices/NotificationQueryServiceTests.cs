using Application.Notification.Contracts;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Notification.QueryServices;
using Tests.TestInfrastructure.Base;

namespace Tests.Infrastructure.Notification.QueryServices;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class NotificationQueryServiceTests(PostgresContainerFixture fixture) : IntegrationTestBase(fixture)
{
    private INotificationQueryService _sut = null!;

    protected override Task OnInitializeAsync()
    {
        _sut = new NotificationQueryService(Context);
        return Task.CompletedTask;
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
        var targetUser = await SeedUserAsync();
        var otherUser = await SeedUserAsync();

        var forTarget1 = new NotificationBuilder()
            .WithUserId(targetUser.Id)
            .WithTitle("Target Order")
            .WithMessage("Your order was placed")
            .Build();
        var forTarget2 = new NotificationBuilder()
            .WithUserId(targetUser.Id)
            .WithTitle("Target Shipping")
            .WithMessage("Your order was shipped")
            .WithType(NotificationType.OrderShipped)
            .Build();
        var forOther = new NotificationBuilder()
            .WithUserId(otherUser.Id)
            .WithTitle("Other")
            .WithMessage("Other user message")
            .Build();

        Context.Notifications.AddRange(forTarget1, forTarget2, forOther);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var result = await _sut.GetByUserIdAsync(targetUser.Id, 1, 10);

        result.TotalCount.ShouldBe(2);
        result.Items.Count.ShouldBe(2);
        result.Items.ShouldAllBe(n => n.UserId == targetUser.Id.Value);
    }

    [Fact]
    public async Task GetByUserIdAsync_MappedFields_MatchAggregate()
    {
        var user = await SeedUserAsync();
        var notification = new NotificationBuilder()
            .WithUserId(user.Id)
            .WithTitle("Custom Title")
            .WithMessage("Custom Message")
            .WithType(NotificationType.OrderPaid)
            .WithActionUrl("/orders/123")
            .Build();

        Context.Notifications.Add(notification);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var result = await _sut.GetByUserIdAsync(user.Id, 1, 10);

        result.Items.Count.ShouldBe(1);
        var dto = result.Items[0];
        dto.UserId.ShouldBe(user.Id.Value);
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
        var user = await SeedUserAsync();
        var first = new NotificationBuilder().WithUserId(user.Id).WithTitle("First").Build();
        await Task.Delay(20);
        var second = new NotificationBuilder().WithUserId(user.Id).WithTitle("Second").Build();
        await Task.Delay(20);
        var third = new NotificationBuilder().WithUserId(user.Id).WithTitle("Third").Build();

        Context.Notifications.AddRange(first, second, third);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var result = await _sut.GetByUserIdAsync(user.Id, 1, 10);

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
        var user = await SeedUserAsync();
        for (var i = 0; i < 4; i++)
        {
            Context.Notifications.Add(
                new NotificationBuilder()
                    .WithUserId(user.Id)
                    .WithTitle($"Title {i}")
                    .Build());
            await Task.Delay(10);
        }
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var result = await _sut.GetByUserIdAsync(user.Id, page, pageSize);

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
        var user = await SeedUserAsync();
        var read1 = new NotificationBuilder().WithUserId(user.Id).WithTitle("Read1").Build();
        read1.MarkAsRead();
        var read2 = new NotificationBuilder().WithUserId(user.Id).WithTitle("Read2").Build();
        read2.MarkAsRead();
        var unread1 = new NotificationBuilder().WithUserId(user.Id).WithTitle("Unread1").Build();
        var unread2 = new NotificationBuilder().WithUserId(user.Id).WithTitle("Unread2").Build();
        var unread3 = new NotificationBuilder().WithUserId(user.Id).WithTitle("Unread3").Build();

        Context.Notifications.AddRange(read1, read2, unread1, unread2, unread3);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var result = await _sut.GetUnreadCountAsync(user.Id);

        result.ShouldBe(3);
    }

    [Fact]
    public async Task GetUnreadCountAsync_OtherUsersUnread_NotCounted()
    {
        var user = await SeedUserAsync();
        var otherUser = await SeedUserAsync();
        Context.Notifications.Add(new NotificationBuilder().WithUserId(user.Id).Build());
        Context.Notifications.Add(new NotificationBuilder().WithUserId(otherUser.Id).Build());
        Context.Notifications.Add(new NotificationBuilder().WithUserId(otherUser.Id).Build());
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var result = await _sut.GetUnreadCountAsync(user.Id);

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
        var user1 = await SeedUserAsync();
        var user2 = await SeedUserAsync();
        Context.Notifications.Add(new NotificationBuilder().WithUserId(user1.Id).Build());
        Context.Notifications.Add(new NotificationBuilder().WithUserId(user2.Id).Build());
        Context.Notifications.Add(new NotificationBuilder().WithUserId(user2.Id).Build());
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var result = await _sut.GetAllAsync(1, 10);

        result.TotalCount.ShouldBe(3);
        result.Items.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetAllAsync_Ordering_ReturnsNewestFirst()
    {
        var user = await SeedUserAsync();
        var older = new NotificationBuilder().WithUserId(user.Id).WithTitle("older").Build();
        var newer = new NotificationBuilder().WithUserId(user.Id).WithTitle("newer").Build();

        Context.Notifications.AddRange(older, newer);
        await Context.SaveChangesAsync();

        var olderDate = new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc);
        var newerDate = new DateTime(2026, 5, 2, 8, 0, 0, DateTimeKind.Utc);
        await Context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"Notifications\" SET \"CreatedAt\" = {olderDate} WHERE \"Id\" = {older.Id.Value}");
        await Context.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE \"Notifications\" SET \"CreatedAt\" = {newerDate} WHERE \"Id\" = {newer.Id.Value}");
        Context.ChangeTracker.Clear();

        var result = await _sut.GetAllAsync(1, 10);

        result.Items[0].Title.ShouldBe("newer");
        result.Items[1].Title.ShouldBe("older");
    }
}
