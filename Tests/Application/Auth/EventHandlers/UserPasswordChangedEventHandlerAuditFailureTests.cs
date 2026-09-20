using Application.Auth.EventHandlers;
using Application.Common.Events;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.User.Events;
using Domain.User.ValueObjects;

namespace Tests.Application.Auth.EventHandlers;

public class UserPasswordChangedEventHandlerAuditFailureTests
{
    private readonly ISessionRepository _sessionRepository = Substitute.For<ISessionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private readonly ILogger<UserPasswordChangedEventHandler> _logger = Substitute.For<ILogger<UserPasswordChangedEventHandler>>();
    private readonly UserPasswordChangedEventHandler _sut;

    public UserPasswordChangedEventHandlerAuditFailureTests()
    {
        _sut = new UserPasswordChangedEventHandler(_sessionRepository, _unitOfWork, _auditService, _logger);
    }

    [Fact]
    public async Task Handle_WhenSecurityAuditLogThrows_LogsSystemFailureAndDoesNotRethrow()
    {
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserPasswordChangedEvent>(new UserPasswordChangedEvent(userId));
        _auditService.LogSecurityEventAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<IpAddress>(), Arg.Any<UserId?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("audit down"));

        await Should.NotThrowAsync(() => _sut.Handle(notification, CancellationToken.None));

        await _sessionRepository.Received(1).RevokeAllByUserIdAsync(userId, SessionRevocationReason.PasswordChanged, Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogSystemEventAsync(
            "PasswordChangedSessionRevocationFailed",
            Arg.Is<string>(s => s.Contains(userId.Value.ToString())),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAuditFailsAfterRevocationFailure_StillDoesNotRethrowOriginal()
    {
        var userId = UserId.NewId();
        var notification = new DomainEventNotification<UserPasswordChangedEvent>(new UserPasswordChangedEvent(userId));
        _sessionRepository.RevokeAllByUserIdAsync(Arg.Any<UserId>(), Arg.Any<SessionRevocationReason>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("revoke down"));

        await Should.NotThrowAsync(() => _sut.Handle(notification, CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        await _auditService.Received(1).LogSystemEventAsync(
            "PasswordChangedSessionRevocationFailed",
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }
}
