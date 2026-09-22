using Application.Common.Events;
using Domain.User.Events;
using Domain.User.ValueObjects;

namespace Tests.Application.Common.Events;

public class DomainEventNotificationTests
{
    [Fact]
    public void Constructor_StoresDomainEvent()
    {
        var userId = UserId.NewId();
        var domainEvent = new UserPasswordChangedEvent(userId);

        var notification = new DomainEventNotification<UserPasswordChangedEvent>(domainEvent);

        notification.DomainEvent.ShouldBeSameAs(domainEvent);
    }

    [Fact]
    public void Notification_ImplementsMediatRINotification()
    {
        var notification = new DomainEventNotification<UserPasswordChangedEvent>(
            new UserPasswordChangedEvent(UserId.NewId()));

        notification.ShouldBeAssignableTo<INotification>();
    }

    [Fact]
    public void Records_WithSameEventInstance_AreEqual()
    {
        var domainEvent = new UserPasswordChangedEvent(UserId.NewId());
        var a = new DomainEventNotification<UserPasswordChangedEvent>(domainEvent);
        var b = new DomainEventNotification<UserPasswordChangedEvent>(domainEvent);

        a.ShouldBe(b);
    }

    [Fact]
    public void Records_WithDifferentEvents_AreNotEqual()
    {
        var a = new DomainEventNotification<UserPasswordChangedEvent>(
            new UserPasswordChangedEvent(UserId.NewId()));
        var b = new DomainEventNotification<UserPasswordChangedEvent>(
            new UserPasswordChangedEvent(UserId.NewId()));

        a.ShouldNotBe(b);
    }

    [Fact]
    public void WithExpression_PreservesTypeAndReplacesEvent()
    {
        var original = new DomainEventNotification<UserPasswordChangedEvent>(
            new UserPasswordChangedEvent(UserId.NewId()));
        var replacement = new UserPasswordChangedEvent(UserId.NewId());

        var updated = original with { DomainEvent = replacement };

        updated.DomainEvent.ShouldBeSameAs(replacement);
    }
}
