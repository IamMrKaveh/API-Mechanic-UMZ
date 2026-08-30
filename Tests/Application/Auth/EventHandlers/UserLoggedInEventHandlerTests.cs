using Application.Auth.EventHandlers;
using Application.Common.Events;
using Domain.Security.Events;
using Domain.User.ValueObjects;

namespace Tests.Application.Auth.EventHandlers;

public class UserLoggedInEventHandlerTests
{
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ILogger<UserLoggedInEventHandler> _logger = Substitute.For<ILogger<UserLoggedInEventHandler>>();
    private readonly UserLoggedInEventHandler _sut;

    public UserLoggedInEventHandlerTests()
    {
        _sut = new UserLoggedInEventHandler(_auditService, _logger);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsSystemEventWithUserId()
    {
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserLoggedInEvent>(new UserLoggedInEvent(userId));

        await _sut.Handle(notification, CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            "User login",
            Arg.Is<string>(s => s!.Contains(userId.Value.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UsesCorrectAuditActionName()
    {
        var notification = new DomainEventNotification<UserLoggedInEvent>(new UserLoggedInEvent(UserId.NewId()));

        await _sut.Handle(notification, CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            "User login",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToAuditService()
    {
        using var cts = new CancellationTokenSource();
        var notification = new DomainEventNotification<UserLoggedInEvent>(new UserLoggedInEvent(UserId.NewId()));

        await _sut.Handle(notification, cts.Token);

        await _auditService.Received(1).LogSystemEventAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            cts.Token);
    }

    [Fact]
    public async Task Handle_InvokesAuditServiceExactlyOnce()
    {
        var notification = new DomainEventNotification<UserLoggedInEvent>(new UserLoggedInEvent(UserId.NewId()));

        await _sut.Handle(notification, CancellationToken.None);

        _auditService.ReceivedCalls().Count().ShouldBe(1);
    }

    [Fact]
    public async Task Handle_MultipleInvocations_LogsEachOne()
    {
        var user1 = UserId.NewId();
        var user2 = UserId.NewId();

        await _sut.Handle(new DomainEventNotification<UserLoggedInEvent>(new UserLoggedInEvent(user1)), CancellationToken.None);
        await _sut.Handle(new DomainEventNotification<UserLoggedInEvent>(new UserLoggedInEvent(user2)), CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            "User login",
            Arg.Is<string>(s => s!.Contains(user1.Value.ToString())),
            Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogSystemEventAsync(
            "User login",
            Arg.Is<string>(s => s!.Contains(user2.Value.ToString())),
            Arg.Any<CancellationToken>());
    }
}
