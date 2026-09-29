using Application.Common.Interfaces;
using Application.Notification.Contracts;
using Application.Notification.Features.Commands.MarkNotificationRead;
using Domain.Notification.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Notification.Features.Commands.MarkNotificationRead;

public class MarkNotificationReadHandlerTests : HandlerTestBase
{
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>(); private readonly MarkNotificationReadHandler _sut;

    public MarkNotificationReadHandlerTests()
    {
        _sut = new MarkNotificationReadHandler(_notificationService, CurrentUserService);
    }

    [Fact]
    public async Task Handle_WhenAuthenticated_ReturnsSuccess()
    {
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());

        var result = await _sut.Handle(
            new MarkNotificationReadCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_WhenAuthenticated_DelegatesToNotificationServiceWithCommandIdAndCurrentUserId()
    {
        var currentUserId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();
        CurrentUserService.UserId.Returns((Guid?)currentUserId);

        await _sut.Handle(
            new MarkNotificationReadCommand(notificationId),
            CancellationToken.None);

        await _notificationService.Received(1).MarkAsReadAsync(
            Arg.Is<NotificationId>(n => n == NotificationId.From(notificationId)),
            Arg.Is<UserId>(u => u == UserId.From(currentUserId)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEmptyNotificationId_ThrowsDomainException()
    {
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());

        await Should.ThrowAsync<DomainException>(() =>
            _sut.Handle(new MarkNotificationReadCommand(Guid.Empty), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsEmpty_ThrowsDomainException()
    {
        CurrentUserService.UserId.Returns((Guid?)Guid.Empty);

        await Should.ThrowAsync<DomainException>(() =>
            _sut.Handle(new MarkNotificationReadCommand(Guid.NewGuid()), CancellationToken.None));
    }
}
