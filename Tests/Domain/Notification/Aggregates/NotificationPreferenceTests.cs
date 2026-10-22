using Domain.Notification.Aggregates;
using Domain.Notification.Enums;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;

namespace Tests.Domain.Notification.Aggregates;

public class NotificationPreferenceTests
{
    private static NotificationPreference Build()
        => NotificationPreference.Create(
            NotificationPreferenceId.NewId(),
            UserId.NewId(),
            new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));

    [Fact]
    public void Create_WithoutDisabledTypes_EnablesEveryNotification()
    {
        var sut = Build();

        sut.IsEnabled(NotificationType.OrderCreated).ShouldBeTrue();
        sut.IsEnabled(NotificationType.DiscountCode).ShouldBeTrue();
        sut.DisabledTypeNames.ShouldBeEmpty();
    }

    [Fact]
    public void Disable_TurnsOffOnlyThatType()
    {
        var sut = Build();

        sut.Disable(NotificationType.OrderCreated, DateTime.UtcNow);

        sut.IsEnabled(NotificationType.OrderCreated).ShouldBeFalse();
        sut.IsEnabled(NotificationType.OrderPaid).ShouldBeTrue();
        sut.DisabledTypeNames.ShouldContain(NotificationType.OrderCreated.Value);
    }

    [Fact]
    public void Disable_Twice_KeepsOneEntry()
    {
        var sut = Build();

        sut.Disable(NotificationType.OrderCreated, DateTime.UtcNow);
        sut.Disable(NotificationType.OrderCreated, DateTime.UtcNow);

        sut.DisabledTypeNames.Count.ShouldBe(1);
    }

    [Fact]
    public void Enable_TurnsTheTypeBackOn()
    {
        var sut = Build();
        sut.Disable(NotificationType.OrderCreated, DateTime.UtcNow);

        sut.Enable(NotificationType.OrderCreated, DateTime.UtcNow);

        sut.IsEnabled(NotificationType.OrderCreated).ShouldBeTrue();
        sut.DisabledTypeNames.ShouldBeEmpty();
    }

    [Fact]
    public void DisableCategory_TurnsOffEveryTypeOfThatCategory()
    {
        var sut = Build();

        sut.DisableCategory(NotificationCategory.Marketing, DateTime.UtcNow);

        sut.IsEnabled(NotificationType.DiscountCode).ShouldBeFalse();
        sut.IsEnabled(NotificationType.OrderCreated).ShouldBeTrue();
    }

    [Fact]
    public void EnableCategory_TurnsEveryTypeOfThatCategoryBackOn()
    {
        var sut = Build();
        sut.DisableCategory(NotificationCategory.Order, DateTime.UtcNow);

        sut.EnableCategory(NotificationCategory.Order, DateTime.UtcNow);

        sut.IsEnabled(NotificationType.OrderCreated).ShouldBeTrue();
        sut.IsEnabled(NotificationType.OrderPaid).ShouldBeTrue();
    }

    [Fact]
    public void ReplaceDisabledTypes_ReplacesTheWholeSetAndRemovesDuplicates()
    {
        var sut = Build();
        sut.Disable(NotificationType.OrderShipped, DateTime.UtcNow);

        sut.ReplaceDisabledTypes(
            [NotificationType.OrderCreated, NotificationType.OrderCreated, NotificationType.DiscountCode],
            DateTime.UtcNow);

        sut.DisabledTypeNames.Count.ShouldBe(2);
        sut.IsEnabled(NotificationType.OrderShipped).ShouldBeTrue();
        sut.IsEnabled(NotificationType.OrderCreated).ShouldBeFalse();
        sut.IsEnabled(NotificationType.DiscountCode).ShouldBeFalse();
    }

    [Fact]
    public void Disable_WithNullType_Throws()
    {
        var sut = Build();

        Should.Throw<ArgumentNullException>(() => sut.Disable(null!, DateTime.UtcNow));
    }
}
