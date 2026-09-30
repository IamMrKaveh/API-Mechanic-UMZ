using Application.Auth.EventHandlers;
using Application.Common.Events;
using Domain.User.Events;
using Domain.User.ValueObjects;

namespace Tests.Application.Auth.EventHandlers;

public class UserDeactivatedEventHandlerTests : HandlerTestBase
{
    private readonly ILogger<UserDeactivatedEventHandler> _logger = Substitute.For<ILogger<UserDeactivatedEventHandler>>();
    private readonly UserDeactivatedEventHandler _sut;

    public UserDeactivatedEventHandlerTests()
    {
        _sut = new UserDeactivatedEventHandler(AuditService, _logger);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsSystemEventWithUserId()
    {
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserDeactivatedEvent>(new UserDeactivatedEvent(userId));

        await _sut.Handle(notification, CancellationToken.None);

        await AuditService.Received(1).LogSystemEventAsync(
            "Deactive User",
            Arg.Is<string>(s => s!.Contains(userId.Value.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToAuditService()
    {
        using var cts = new CancellationTokenSource();
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserDeactivatedEvent>(new UserDeactivatedEvent(userId));

        await _sut.Handle(notification, cts.Token);

        await AuditService.Received(1).LogSystemEventAsync(Arg.Any<string>(), Arg.Any<string>(), cts.Token);
    }

    [Fact]
    public async Task Handle_LogsAuditEventExactlyOnce()
    {
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserDeactivatedEvent>(new UserDeactivatedEvent(userId));

        await _sut.Handle(notification, CancellationToken.None);

        AuditService.ReceivedCalls().Count().ShouldBe(1);
    }
}
