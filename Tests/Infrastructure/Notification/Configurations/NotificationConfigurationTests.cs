using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Tests.TestInfrastructure.Builders;
using Tests.TestInfrastructure.Database;
using Xunit;

namespace Tests.Infrastructure.Notification.Configurations;

[Trait("Category", "Integration")]
[Collection(nameof(DatabaseCollection))]
public class NotificationConfigurationTests(PostgresContainerFixture fixture) : IAsyncLifetime
{
    private readonly PostgresContainerFixture _fixture = fixture;
    private DBContext _context = null!;

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

    private async Task<UserId> SeedUserAsync()
    {
        var user = new UserBuilder().Build();
        user.ClearDomainEvents();
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task Persist_NewNotification_RoundTripsAllMappedProperties()
    {
        var userId = await SeedUserAsync();
        var relatedEntityId = Guid.NewGuid();

        var notification = new NotificationBuilder()
            .WithUserId(userId)
            .WithType(NotificationType.OrderCreated)
            .WithTitle("Order placed")
            .WithMessage("Your order has been placed successfully.")
            .WithActionUrl("/orders/123")
            .WithRelatedEntity("Order", relatedEntityId)
            .Build();
        notification.ClearDomainEvents();

        await _context.Notifications.AddAsync(notification);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var reloaded = await freshContext.Notifications.SingleAsync(n => n.Id == notification.Id);

        reloaded.Id.ShouldBe(notification.Id);
        reloaded.UserId.ShouldBe(userId);
        reloaded.Title.ShouldBe("Order placed");
        reloaded.Message.ShouldBe("Your order has been placed successfully.");
        reloaded.Type.Value.ShouldBe(NotificationType.OrderCreated.Value);
        reloaded.ActionUrl.ShouldBe("/orders/123");
        reloaded.RelatedEntityType.ShouldBe("Order");
        reloaded.RelatedEntityId.ShouldBe(relatedEntityId);
        reloaded.IsRead.ShouldBeFalse();
        reloaded.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public async Task Persist_NotificationWithoutOptionalFields_StoresNullValues()
    {
        var userId = await SeedUserAsync();

        var notification = new NotificationBuilder()
            .WithUserId(userId)
            .WithType(NotificationType.SystemAlert)
            .WithTitle("System")
            .WithMessage("Message")
            .WithActionUrl(null)
            .WithRelatedEntity(null, null)
            .Build();
        notification.ClearDomainEvents();

        await _context.Notifications.AddAsync(notification);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var reloaded = await freshContext.Notifications.SingleAsync(n => n.Id == notification.Id);

        reloaded.ActionUrl.ShouldBeNull();
        reloaded.RelatedEntityType.ShouldBeNull();
        reloaded.RelatedEntityId.ShouldBeNull();
    }

    [Theory]
    [InlineData("OrderCreated")]
    [InlineData("OrderPaid")]
    [InlineData("OrderShipped")]
    [InlineData("OrderDelivered")]
    [InlineData("OrderCancelled")]
    [InlineData("TicketReply")]
    [InlineData("PriceDropAlert")]
    [InlineData("StockAlert")]
    [InlineData("DiscountCode")]
    [InlineData("SystemAlert")]
    [InlineData("SecurityAlert")]
    [InlineData("AccountUpdate")]
    public async Task Persist_NotificationType_IsPreservedThroughValueConversion(string typeValue)
    {
        var userId = await SeedUserAsync();
        var type = NotificationType.FromString(typeValue);

        var notification = new NotificationBuilder()
            .WithUserId(userId)
            .WithType(type)
            .Build();
        notification.ClearDomainEvents();

        await _context.Notifications.AddAsync(notification);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var reloaded = await freshContext.Notifications.SingleAsync(n => n.Id == notification.Id);

        reloaded.Type.Value.ShouldBe(type.Value);
    }

    [Fact]
    public async Task Persist_MarkAsRead_PersistsIsReadFlagAndUpdatedAt()
    {
        var userId = await SeedUserAsync();

        var notification = new NotificationBuilder()
            .WithUserId(userId)
            .WithType(NotificationType.OrderCreated)
            .Build();
        notification.ClearDomainEvents();

        await _context.Notifications.AddAsync(notification);
        await _context.SaveChangesAsync();

        notification.MarkAsRead();
        notification.ClearDomainEvents();
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();
        var reloaded = await freshContext.Notifications.SingleAsync(n => n.Id == notification.Id);

        reloaded.IsRead.ShouldBeTrue();
        reloaded.UpdatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Query_NotificationsByUserId_ReturnsOnlyMatchingNotifications()
    {
        var firstUser = await SeedUserAsync();
        var secondUser = await SeedUserAsync();

        var firstUserNotification = new NotificationBuilder().WithUserId(firstUser).Build();
        firstUserNotification.ClearDomainEvents();

        var secondUserNotification = new NotificationBuilder().WithUserId(secondUser).Build();
        secondUserNotification.ClearDomainEvents();

        await _context.Notifications.AddRangeAsync(firstUserNotification, secondUserNotification);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();

        var results = await freshContext.Notifications
            .Where(n => n.UserId == firstUser)
            .ToListAsync();

        results.Count.ShouldBe(1);
        results[0].Id.ShouldBe(firstUserNotification.Id);
    }

    [Fact]
    public async Task Persist_MultipleNotificationsForSameUser_AreAllPersisted()
    {
        var userId = await SeedUserAsync();

        var notifications = Enumerable.Range(0, 5)
            .Select(_ =>
            {
                var n = new NotificationBuilder().WithUserId(userId).Build();
                n.ClearDomainEvents();
                return n;
            })
            .ToList();

        await _context.Notifications.AddRangeAsync(notifications);
        await _context.SaveChangesAsync();

        await using var freshContext = _fixture.CreateContext();

        var count = await freshContext.Notifications
            .CountAsync(n => n.UserId == userId);

        count.ShouldBe(5);
    }
}
