using Domain.Notification.Aggregates;
using Domain.Notification.Interfaces;
using Domain.Notification.ValueObjects;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Notification.Services;
using SharedKernel.Exceptions;

namespace Tests.Infrastructure.Notification.Services;

public class NotificationServiceTests
{
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly NotificationService _sut;

    public NotificationServiceTests()
    {
        _sut = new NotificationService(_notificationRepository, _auditService, _unitOfWork);
    }

    private static global::Domain.Notification.Aggregates.Notification NewNotification(UserId userId, string title = "Title") =>
        new NotificationBuilder()
            .WithUserId(userId)
            .WithType(NotificationType.OrderCreated)
            .WithTitle(title)
            .WithMessage("Message body")
            .Build();

    [Fact]
    public async Task CreateNotificationAsync_PersistsNotificationAndLogsSystemEvent()
    {
        var userId = UserId.NewId();
        global::Domain.Notification.Aggregates.Notification? captured = null;
        await _notificationRepository.AddAsync(Arg.Do<global::Domain.Notification.Aggregates.Notification>(n => captured = n), Arg.Any<CancellationToken>());

        await _sut.CreateNotificationAsync(
            userId, "Order placed", "Your order was placed.", "OrderCreated",
            "/orders/1", Guid.NewGuid(), "Order", CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.UserId.ShouldBe(userId);
        captured.Title.ShouldBe("Order placed");
        captured.Type.Value.ShouldBe(NotificationType.OrderCreated.Value);
        captured.ActionUrl.ShouldBe("/orders/1");
        captured.RelatedEntityType.ShouldBe("Order");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogSystemEventAsync(
            "NotificationCreated", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateNotificationAsync_MapsAllSupportedTypes()
    {
        var userId = UserId.NewId();
        var capturedTypes = new List<string>();
        await _notificationRepository.AddAsync(
            Arg.Do<global::Domain.Notification.Aggregates.Notification>(n => capturedTypes.Add(n.Type.Value)), Arg.Any<CancellationToken>());

        foreach (var type in new[] { "OrderCreated", "OrderPaid", "OrderShipped", "SystemAlert" })
            await _sut.CreateNotificationAsync(userId, "T", "M", type, ct: CancellationToken.None);

        capturedTypes.ShouldBe(["OrderCreated", "OrderPaid", "OrderShipped", "SystemAlert"]);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationExists_MarksReadAndSaves()
    {
        var userId = UserId.NewId();
        var notification = NewNotification(userId);
        _notificationRepository.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(notification);

        await _sut.MarkAsReadAsync(notification.Id, userId, CancellationToken.None);

        notification.IsRead.ShouldBeTrue();
        _notificationRepository.Received(1).Update(notification);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenNotificationDoesNotExist_DoesNothing()
    {
        _notificationRepository.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns((global::Domain.Notification.Aggregates.Notification?)null);

        await _sut.MarkAsReadAsync(NotificationId.NewId(), UserId.NewId(), CancellationToken.None);

        _notificationRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task MarkAsReadAsync_WhenUserIsNotOwner_ThrowsDomainException()
    {
        var notification = NewNotification(UserId.NewId());
        _notificationRepository.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(notification);

        await Should.ThrowAsync<DomainException>(() =>
            _sut.MarkAsReadAsync(notification.Id, UserId.NewId(), CancellationToken.None));

        await _unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_MarksEveryUnreadNotification()
    {
        var userId = UserId.NewId();
        var first = NewNotification(userId, "First");
        var second = NewNotification(userId, "Second");
        _notificationRepository.GetUnreadByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns([first, second]);

        await _sut.MarkAllAsReadAsync(userId, CancellationToken.None);

        first.IsRead.ShouldBeTrue();
        second.IsRead.ShouldBeTrue();
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAllAsReadAsync_WhenNoneUnread_StillSaves()
    {
        _notificationRepository.GetUnreadByUserIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns([]);

        await _sut.MarkAllAsReadAsync(UserId.NewId(), CancellationToken.None);

        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenNotificationExists_RemovesAndSaves()
    {
        var userId = UserId.NewId();
        var notification = NewNotification(userId);
        _notificationRepository.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(notification);

        await _sut.DeleteAsync(notification.Id, userId, CancellationToken.None);

        _notificationRepository.Received(1).Remove(notification);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenNotificationDoesNotExist_DoesNothing()
    {
        _notificationRepository.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns((global::Domain.Notification.Aggregates.Notification?)null);

        await _sut.DeleteAsync(NotificationId.NewId(), UserId.NewId(), CancellationToken.None);

        _notificationRepository.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Fact]
    public async Task DeleteAsync_WhenUserIsNotOwner_ThrowsDomainException()
    {
        var notification = NewNotification(UserId.NewId());
        _notificationRepository.GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(notification);

        await Should.ThrowAsync<DomainException>(() =>
            _sut.DeleteAsync(notification.Id, UserId.NewId(), CancellationToken.None));

        _notificationRepository.DidNotReceiveWithAnyArgs().Remove(default!);
    }

    [Theory]
    [InlineData("Paid", "OrderPaid")]
    [InlineData("Shipped", "OrderShipped")]
    [InlineData("Delivered", "OrderDelivered")]
    [InlineData("Cancelled", "OrderCancelled")]
    [InlineData("Created", "OrderCreated")]
    [InlineData("SomethingElse", "OrderCreated")]
    public async Task SendOrderStatusNotificationAsync_MapsStatusToType(string status, string expectedType)
    {
        var userId = UserId.NewId();
        var orderId = OrderId.NewId();
        global::Domain.Notification.Aggregates.Notification? captured = null;
        await _notificationRepository.AddAsync(Arg.Do<global::Domain.Notification.Aggregates.Notification>(n => captured = n), Arg.Any<CancellationToken>());

        await _sut.SendOrderStatusNotificationAsync(userId, orderId, "Created", status, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Type.Value.ShouldBe(expectedType);
        captured.ActionUrl.ShouldBe($"/dashboard/orders/{orderId.Value}");
        captured.RelatedEntityId.ShouldBe(orderId.Value);
        captured.RelatedEntityType.ShouldBe("Order");
    }
}
