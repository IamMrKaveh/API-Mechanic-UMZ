using Application.Auth.EventHandlers;
using Application.Common.Events;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.User.Events;
using Domain.User.ValueObjects;

namespace Tests.Application.Auth.EventHandlers;

public class UserPasswordChangedEventHandlerTests
{
    private readonly ISessionRepository _sessionRepository = Substitute.For<ISessionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ILogger<UserPasswordChangedEventHandler> _logger = Substitute.For<ILogger<UserPasswordChangedEventHandler>>();
    private readonly UserPasswordChangedEventHandler _sut;

    public UserPasswordChangedEventHandlerTests()
    {
        _sut = new UserPasswordChangedEventHandler(_sessionRepository, _unitOfWork, _auditService, _logger);
    }

    [Fact]
    public async Task Handle_WhenValid_RevokesAllSessionsWithPasswordChangedReason()
    {
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserPasswordChangedEvent>(new UserPasswordChangedEvent(userId));

        await _sut.Handle(notification, CancellationToken.None);

        await _sessionRepository.Received(1).RevokeAllByUserIdAsync(
            userId,
            SessionRevocationReason.PasswordChanged,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenValid_SavesChangesAfterRevocation()
    {
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserPasswordChangedEvent>(new UserPasswordChangedEvent(userId));

        await _sut.Handle(notification, CancellationToken.None);

        Received.InOrder(() =>
        {
            _sessionRepository.RevokeAllByUserIdAsync(
                userId,
                SessionRevocationReason.PasswordChanged,
                Arg.Any<CancellationToken>());
            _unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_WhenValid_LogsSecurityEventWithUnknownIpAndUserId()
    {
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserPasswordChangedEvent>(new UserPasswordChangedEvent(userId));

        await _sut.Handle(notification, CancellationToken.None);

        await _auditService.Received(1).LogSecurityEventAsync(
            "PasswordChanged",
            Arg.Is<string>(s => s!.Contains(userId.Value.ToString())),
            Arg.Is<IpAddress>(ip => ip == IpAddress.Unknown),
            userId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenRevocationThrows_LogsFailureAndDoesNotRethrow()
    {
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserPasswordChangedEvent>(new UserPasswordChangedEvent(userId));
        var exception = new InvalidOperationException("revoke failure");

        _sessionRepository
            .RevokeAllByUserIdAsync(
                Arg.Any<UserId>(),
                Arg.Any<SessionRevocationReason>(),
                Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);

        await Should.NotThrowAsync(() => _sut.Handle(notification, CancellationToken.None));

        await _auditService.Received(1).LogSystemEventAsync(
            "PasswordChangedSessionRevocationFailed",
            Arg.Is<string>(s => s!.Contains(userId.Value.ToString()) && s.Contains(exception.Message)),
            Arg.Any<CancellationToken>());
        await _auditService.DidNotReceive().LogSecurityEventAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IpAddress>(),
            Arg.Any<UserId?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSaveChangesThrows_LogsFailureAndDoesNotRethrow()
    {
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserPasswordChangedEvent>(new UserPasswordChangedEvent(userId));
        var exception = new InvalidOperationException("save failed");

        _unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);

        await Should.NotThrowAsync(() => _sut.Handle(notification, CancellationToken.None));

        await _auditService.Received(1).LogSystemEventAsync(
            "PasswordChangedSessionRevocationFailed",
            Arg.Is<string>(s => s!.Contains(exception.Message)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSuccessful_DoesNotLogSystemFailureEvent()
    {
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserPasswordChangedEvent>(new UserPasswordChangedEvent(userId));

        await _sut.Handle(notification, CancellationToken.None);

        await _auditService.DidNotReceive().LogSystemEventAsync(
            "PasswordChangedSessionRevocationFailed",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenDownstream()
    {
        using var cts = new CancellationTokenSource();
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserPasswordChangedEvent>(new UserPasswordChangedEvent(userId));

        await _sut.Handle(notification, cts.Token);

        await _sessionRepository.Received(1).RevokeAllByUserIdAsync(
            userId,
            SessionRevocationReason.PasswordChanged,
            cts.Token);
        await _unitOfWork.Received(1).SaveChangesAsync(cts.Token);
        await _auditService.Received(1).LogSecurityEventAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<IpAddress>(),
            Arg.Any<UserId?>(),
            cts.Token);
    }
}
