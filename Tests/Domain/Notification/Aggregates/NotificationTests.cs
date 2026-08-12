using Domain.Notification.Events;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Domain.Notification.Aggregates;

public class NotificationTests
{
    [Fact]
    public void Create_WithValidInput_ReturnsInitializedNotificationWithProvidedIdentifiers()
    {
        var id = NotificationId.NewId();
        var userId = UserId.NewId();
        var type = NotificationType.OrderCreated;

        var sut = new NotificationBuilder()
            .WithId(id)
            .WithUserId(userId)
            .WithType(type)
            .WithTitle("عنوان")
            .WithMessage("متن")
            .Build();

        sut.Id.ShouldBe(id);
        sut.UserId.ShouldBe(userId);
        sut.Type.ShouldBe(type);
        sut.Title.ShouldBe("عنوان");
        sut.Message.ShouldBe("متن");
    }

    [Fact]
    public void Create_DefaultsIsReadToFalseAndUpdatedAtToNull()
    {
        var sut = new NotificationBuilder().Build();

        sut.IsRead.ShouldBeFalse();
        sut.UpdatedAt.ShouldBeNull();
    }

    [Fact]
    public void Create_SetsCreatedAtCloseToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var sut = new NotificationBuilder().Build();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.CreatedAt.ShouldBeGreaterThanOrEqualTo(before);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void Create_WithLeadingAndTrailingWhitespaceInTitleAndMessage_TrimsBothFields()
    {
        var sut = new NotificationBuilder()
            .WithTitle("   عنوان   ")
            .WithMessage("   متن   ")
            .Build();

        sut.Title.ShouldBe("عنوان");
        sut.Message.ShouldBe("متن");
    }

    [Fact]
    public void Create_WithOptionalArgs_AssignsActionUrlAndRelatedEntity()
    {
        var relatedId = Guid.NewGuid();

        var sut = new NotificationBuilder()
            .WithActionUrl("https://example.com/orders/1")
            .WithRelatedEntity("Order", relatedId)
            .Build();

        sut.ActionUrl.ShouldBe("https://example.com/orders/1");
        sut.RelatedEntityType.ShouldBe("Order");
        sut.RelatedEntityId.ShouldBe(relatedId);
    }

    [Fact]
    public void Create_WithoutOptionalArgs_LeavesActionUrlAndRelatedEntityNull()
    {
        var sut = new NotificationBuilder()
            .WithActionUrl(null)
            .WithRelatedEntity(null, null)
            .Build();

        sut.ActionUrl.ShouldBeNull();
        sut.RelatedEntityType.ShouldBeNull();
        sut.RelatedEntityId.ShouldBeNull();
    }

    [Fact]
    public void Create_RaisesExactlyOneNotificationCreatedEventCarryingIdUserIdAndType()
    {
        var id = NotificationId.NewId();
        var userId = UserId.NewId();
        var type = NotificationType.OrderPaid;

        var sut = new NotificationBuilder()
            .WithId(id)
            .WithUserId(userId)
            .WithType(type)
            .Build();

        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<NotificationCreatedEvent>();
        evt.NotificationId.ShouldBe(id);
        evt.UserId.ShouldBe(userId);
        evt.NotificationType.ShouldBe(type);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespaceTitle_ThrowsDomainException(string? title)
    {
        var ex = Should.Throw<DomainException>(() => new NotificationBuilder().WithTitle(title!).Build());
        ex.Message.ShouldBe("عنوان اعلان الزامی است.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrWhitespaceMessage_ThrowsDomainException(string? message)
    {
        var ex = Should.Throw<DomainException>(() => new NotificationBuilder().WithMessage(message!).Build());
        ex.Message.ShouldBe("متن اعلان الزامی است.");
    }

    [Fact]
    public void EnsureUserAccess_ForOwningUser_DoesNotThrow()
    {
        var owner = UserId.NewId();
        var sut = new NotificationBuilder().WithUserId(owner).Build();

        Should.NotThrow(() => sut.EnsureUserAccess(owner));
    }

    [Fact]
    public void EnsureUserAccess_ForDifferentUser_ThrowsDomainException()
    {
        var sut = new NotificationBuilder().WithUserId(UserId.NewId()).Build();

        var ex = Should.Throw<DomainException>(() => sut.EnsureUserAccess(UserId.NewId()));
        ex.Message.ShouldBe("شما دسترسی به این اعلان را ندارید.");
    }

    [Fact]
    public void MarkAsRead_OnUnreadNotification_FlipsIsReadToTrue()
    {
        var sut = new NotificationBuilder().Build();

        sut.MarkAsRead();

        sut.IsRead.ShouldBeTrue();
    }

    [Fact]
    public void MarkAsRead_OnUnreadNotification_SetsUpdatedAtCloseToUtcNow()
    {
        var sut = new NotificationBuilder().Build();
        var before = DateTime.UtcNow.AddSeconds(-1);

        sut.MarkAsRead();

        var after = DateTime.UtcNow.AddSeconds(1);
        sut.UpdatedAt.ShouldNotBeNull();
        sut.UpdatedAt!.Value.ShouldBeGreaterThanOrEqualTo(before);
        sut.UpdatedAt!.Value.ShouldBeLessThanOrEqualTo(after);
    }

    [Fact]
    public void MarkAsRead_OnUnreadNotification_RaisesExactlyOneNotificationReadEventCarryingSameId()
    {
        var id = NotificationId.NewId();
        var sut = new NotificationBuilder().WithId(id).Build();
        sut.ClearDomainEvents();

        sut.MarkAsRead();

        sut.DomainEvents.Count.ShouldBe(1);
        var evt = sut.DomainEvents.Single().ShouldBeOfType<NotificationReadEvent>();
        evt.NotificationId.ShouldBe(id);
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_IsIdempotentNoOpAndRaisesNoAdditionalEvent()
    {
        var sut = new NotificationBuilder().Build();
        sut.MarkAsRead();
        var firstUpdatedAt = sut.UpdatedAt;
        sut.ClearDomainEvents();

        sut.MarkAsRead();

        sut.IsRead.ShouldBeTrue();
        sut.UpdatedAt.ShouldBe(firstUpdatedAt);
        sut.DomainEvents.ShouldBeEmpty();
    }
}
