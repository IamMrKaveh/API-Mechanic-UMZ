
using Domain.Notification.Events;
using Domain.Notification.ValueObjects;

namespace Tests.Domain.Notification.Events;

public class NotificationReadEventTests
{
    [Fact]
    public void Ctor_AssignsNotificationIdFromArgument()
    {
        var notificationId = NotificationId.NewId();

        var sut = new NotificationReadEvent(notificationId);

        sut.NotificationId.ShouldBe(notificationId);
    }

    [Fact]
    public void Ctor_GeneratesNonEmptyEventIdAndSetsOccurredAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new NotificationReadEvent(NotificationId.NewId());

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.EventId.ShouldNotBe(Guid.Empty);
        sut.OccurredAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.OccurredAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Ctor_TwoInstances_HaveDifferentEventIds()
    {
        var a = new NotificationReadEvent(NotificationId.NewId());
        var b = new NotificationReadEvent(NotificationId.NewId());

        a.EventId.ShouldNotBe(b.EventId);
    }
}

