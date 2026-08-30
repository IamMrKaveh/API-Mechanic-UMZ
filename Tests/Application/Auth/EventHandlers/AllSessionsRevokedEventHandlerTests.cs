using Application.Auth.EventHandlers;
using Application.Common.Events;
using Domain.Security.Enums;
using Domain.Security.Events;
using Domain.User.ValueObjects;

namespace Tests.Application.Auth.EventHandlers;

public class AllSessionsRevokedEventHandlerTests
{
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ILogger<AllSessionsRevokedEventHandler> _logger = Substitute.For<ILogger<AllSessionsRevokedEventHandler>>();
    private readonly AllSessionsRevokedEventHandler _sut;

    public AllSessionsRevokedEventHandlerTests()
    {
        _sut = new AllSessionsRevokedEventHandler(_auditService, _logger);
    }

    [Fact]
    public async Task Handle_WithValidEvent_LogsSystemEventWithRevokedCountAndReason()
    {
        var userId = UserId.NewId();
        var evt = new AllSessionsRevokedEvent(userId, SessionRevocationReason.UserRequested, 5);
        var notification = new DomainEventNotification<AllSessionsRevokedEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            "All Session Revoked",
            Arg.Is<string>(s => s!.Contains(userId.Value.ToString())
                && s.Contains('5')
                && s.Contains(SessionRevocationReason.UserRequested.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(SessionRevocationReason.UserRequested, 1)]
    [InlineData(SessionRevocationReason.AdminRevoked, 3)]
    [InlineData(SessionRevocationReason.SecurityConcern, 10)]
    [InlineData(SessionRevocationReason.PasswordChanged, 2)]
    [InlineData(SessionRevocationReason.AccountDeactivated, 7)]
    [InlineData(SessionRevocationReason.AllSessionsRevoked, 4)]
    [InlineData(SessionRevocationReason.PhoneChanged, 6)]
    public async Task Handle_WithVariousReasons_LogsAuditSystemEventOnce(SessionRevocationReason reason, int count)
    {
        var userId = UserId.NewId();
        var evt = new AllSessionsRevokedEvent(userId, reason, count);
        var notification = new DomainEventNotification<AllSessionsRevokedEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s!.Contains(reason.ToString()) && s.Contains(count.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithZeroRevokedCount_StillLogsAuditEvent()
    {
        var userId = UserId.NewId();
        var evt = new AllSessionsRevokedEvent(userId, SessionRevocationReason.AllSessionsRevoked, 0);
        var notification = new DomainEventNotification<AllSessionsRevokedEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            "All Session Revoked",
            Arg.Is<string>(s => s!.Contains('0')),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToAuditService()
    {
        using var cts = new CancellationTokenSource();
        var userId = UserId.NewId();
        var evt = new AllSessionsRevokedEvent(userId, SessionRevocationReason.AdminRevoked, 2);
        var notification = new DomainEventNotification<AllSessionsRevokedEvent>(evt);

        await _sut.Handle(notification, cts.Token);

        await _auditService.Received(1).LogSystemEventAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            cts.Token);
    }

    [Fact]
    public async Task Handle_DoesNotCallAuditServiceMoreThanOnce()
    {
        var userId = UserId.NewId();
        var evt = new AllSessionsRevokedEvent(userId, SessionRevocationReason.SecurityConcern, 3);
        var notification = new DomainEventNotification<AllSessionsRevokedEvent>(evt);

        await _sut.Handle(notification, CancellationToken.None);

        await _auditService.Received(1).LogSystemEventAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        _auditService.ReceivedCalls().Count().ShouldBe(1);
    }
}
