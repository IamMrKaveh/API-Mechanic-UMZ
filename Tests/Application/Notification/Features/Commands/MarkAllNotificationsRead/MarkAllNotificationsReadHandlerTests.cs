using Application.Common.Interfaces;
using Application.Notification.Contracts;
using Application.Notification.Features.Commands.MarkAllNotificationsRead;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Notification.Features.Commands.MarkAllNotificationsRead;

public class MarkAllNotificationsReadHandlerTests : HandlerTestBase
{
    private readonly INotificationService _notificationService = Substitute.For<INotificationService>(); private readonly MarkAllNotificationsReadHandler _sut;

    public MarkAllNotificationsReadHandlerTests()
    {
        _sut = new MarkAllNotificationsReadHandler(_notificationService, CurrentUserService);
    }

    [Fact]
    public async Task Handle_WhenAuthenticated_ReturnsSuccess()
    {
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());

        var result = await _sut.Handle(new MarkAllNotificationsReadCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
    }

    [Fact]
    public async Task Handle_WhenAuthenticated_DelegatesToNotificationServiceWithCurrentUserId()
    {
        var currentUserId = Guid.NewGuid();
        CurrentUserService.UserId.Returns((Guid?)currentUserId);

        await _sut.Handle(new MarkAllNotificationsReadCommand(), CancellationToken.None);

        await _notificationService.Received(1).MarkAllAsReadAsync(
            Arg.Is<UserId>(u => u == UserId.From(currentUserId)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIdIsEmpty_ThrowsDomainException()
    {
        CurrentUserService.UserId.Returns((Guid?)Guid.Empty);

        await Should.ThrowAsync<DomainException>(() =>
            _sut.Handle(new MarkAllNotificationsReadCommand(), CancellationToken.None));
    }
}
