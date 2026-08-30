using Application.Auth.EventHandlers;
using Application.Common.Events;
using Domain.Security.Events;
using Domain.User.ValueObjects;

namespace Tests.Application.Auth.EventHandlers;

public class UserLockedOutEventHandlerTests
{
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ILogger<UserLockedOutEventHandler> _logger = Substitute.For<ILogger<UserLockedOutEventHandler>>();
    private readonly UserLockedOutEventHandler _sut;

    public UserLockedOutEventHandlerTests()
    {
        _sut = new UserLockedOutEventHandler(_auditService, _logger);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsSystemEventContainingFailedAttemptsAndLockoutEnd()
    {
        var userId = UserId.NewId();
        var lockoutEnd = DateTime.UtcNow.AddMinutes(30);
        var evt = new UserLockedOutEvent(userId, lockoutEnd, 5);
        var notification = new DomainEventNotification<UserLockedOutEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            "User Locked out",
            Arg.Is<string>(s =>
                s!.Contains(userId.Value.ToString())
                && s.Contains('5')
                && s.Contains(lockoutEnd.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Handle_WithVariousFailedAttemptCounts_IncludesCountInAuditMessage(int failedAttempts)
    {
        var userId = UserId.NewId();
        var evt = new UserLockedOutEvent(userId, DateTime.UtcNow.AddMinutes(15), failedAttempts);
        var notification = new DomainEventNotification<UserLockedOutEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s!.Contains(failedAttempts.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLockoutIsFarInFuture_StillLogsCorrectly()
    {
        var userId = UserId.NewId();
        var lockoutEnd = DateTime.UtcNow.AddYears(1);
        var evt = new UserLockedOutEvent(userId, lockoutEnd, 100);
        var notification = new DomainEventNotification<UserLockedOutEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            "User Locked out",
            Arg.Is<string>(s => s!.Contains("100")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToAuditService()
    {
        using var cts = new CancellationTokenSource();
        var evt = new UserLockedOutEvent(UserId.NewId(), DateTime.UtcNow.AddMinutes(5), 3);
        var notification = new DomainEventNotification<UserLockedOutEvent>(evt);

        await _sut.Handle(notification, cts.Token);

        await _auditService.Received(1).LogSystemEventAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            cts.Token);
    }

    [Fact]
    public async Task Handle_CallsAuditServiceExactlyOnce()
    {
        var evt = new UserLockedOutEvent(UserId.NewId(), DateTime.UtcNow.AddMinutes(10), 4);
        var notification = new DomainEventNotification<UserLockedOutEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        _auditService.ReceivedCalls().Count().ShouldBe(1);
    }
}
