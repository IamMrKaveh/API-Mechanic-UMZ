
using Domain.Notification.Events;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;

namespace Tests.Domain.Notification.Events;

public class NotificationCreatedEventTests
{
    [Fact]
    public void Ctor_AssignsNotificationIdUserIdAndTypeFromArguments()
    {
        var notificationId = NotificationId.NewId();
        var userId = UserId.NewId();
        var type = NotificationType.OrderShipped;

        var sut = new NotificationCreatedEvent(notificationId, userId, type);

        sut.NotificationId.ShouldBe(notificationId);
        sut.UserId.ShouldBe(userId);
        sut.NotificationType.ShouldBe(type);
    }

    [Fact]
    public void Ctor_GeneratesNonEmptyEventIdAndSetsOccurredAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new NotificationCreatedEvent(NotificationId.NewId(), UserId.NewId(), NotificationType.OrderCreated);

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.EventId.ShouldNotBe(Guid.Empty);
        sut.OccurredAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.OccurredAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Ctor_TwoInstances_HaveDifferentEventIds()
    {
        var a = new NotificationCreatedEvent(NotificationId.NewId(), UserId.NewId(), NotificationType.OrderCreated);
        var b = new NotificationCreatedEvent(NotificationId.NewId(), UserId.NewId(), NotificationType.OrderCreated);

        a.EventId.ShouldNotBe(b.EventId);
    }
}

