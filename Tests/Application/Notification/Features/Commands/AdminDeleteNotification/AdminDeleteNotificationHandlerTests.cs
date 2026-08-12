using Application.Notification.Features.Commands.AdminDeleteNotification;
using Domain.Notification.Interfaces;
using Domain.Notification.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Notifications = Domain.Notification.Aggregates.Notification;

namespace Tests.Application.Notification.Features.Commands.AdminDeleteNotification;

public class AdminDeleteNotificationHandlerTests
{
    private readonly INotificationRepository _notificationRepository = Substitute.For<INotificationRepository>(); private readonly AdminDeleteNotificationHandler _sut;

    public AdminDeleteNotificationHandlerTests()
    {
        _sut = new AdminDeleteNotificationHandler(_notificationRepository);
    }

    [Fact]
    public async Task Handle_WhenNotificationNotFound_ReturnsNotFound()
    {
        _notificationRepository
            .GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns((Notifications?)null);

        var result = await _sut.Handle(
            new AdminDeleteNotificationCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        result.ShouldFailWithType(ErrorType.NotFound);
        _notificationRepository.DidNotReceive().Remove(Arg.Any<Notifications>());
    }

    [Fact]
    public async Task Handle_WhenNotificationExists_RemovesNotificationAndReturnsSuccess()
    {
        var notification = new NotificationBuilder().Build();
        _notificationRepository
            .GetByIdAsync(Arg.Any<NotificationId>(), Arg.Any<CancellationToken>())
            .Returns(notification);

        var result = await _sut.Handle(
            new AdminDeleteNotificationCommand(notification.Id.Value),
            CancellationToken.None);

        result.ShouldBeSuccess();
        _notificationRepository.Received(1).Remove(notification);
    }

    [Fact]
    public async Task Handle_WhenNotificationExists_LooksUpUsingCommandNotificationId()
    {
        var notification = new NotificationBuilder().Build();
        NotificationId? captured = null;
        _notificationRepository
            .GetByIdAsync(Arg.Do<NotificationId>(id => captured = id), Arg.Any<CancellationToken>())
            .Returns(notification);

        await _sut.Handle(
            new AdminDeleteNotificationCommand(notification.Id.Value),
            CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(notification.Id.Value);
    }

    [Fact]
    public async Task Handle_WithEmptyNotificationId_ThrowsDomainException()
    {
        var command = new AdminDeleteNotificationCommand(Guid.Empty);

        await Should.ThrowAsync<DomainException>(() =>
            _sut.Handle(command, CancellationToken.None));
    }
}
