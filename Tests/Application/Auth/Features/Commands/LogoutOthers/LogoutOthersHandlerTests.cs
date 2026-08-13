using Application.Auth.Features.Commands.LogoutOthers;
using Application.Common.Interfaces;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Auth.Features.Commands.LogoutOthers;

public class LogoutOthersHandlerTests
{
    private readonly ISessionRepository _sessionRepository = Substitute.For<ISessionRepository>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly LogoutOthersHandler _sut;

    public LogoutOthersHandlerTests()
    {
        _sut = new LogoutOthersHandler(_sessionRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.UserId.Returns((Guid?)null);

        var result = await _sut.Handle(new LogoutOthersCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Unauthorized);
        await _sessionRepository.DidNotReceiveWithAnyArgs().RevokeAllByUserIdAsync(default!, default);
        await _sessionRepository.DidNotReceiveWithAnyArgs().RevokeAllExceptAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenCurrentSessionIdIsNull_RevokesAllForCallerWithUserRequested()
    {
        var callerGuid = Guid.NewGuid();
        _currentUser.UserId.Returns((Guid?)callerGuid);
        _currentUser.SessionId.Returns((Guid?)null);

        var result = await _sut.Handle(new LogoutOthersCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        await _sessionRepository.Received(1).RevokeAllByUserIdAsync(
            Arg.Is<UserId>(x => x == UserId.From(callerGuid)),
            SessionRevocationReason.UserRequested,
            Arg.Any<CancellationToken>());
        await _sessionRepository.DidNotReceiveWithAnyArgs().RevokeAllExceptAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenCurrentSessionIdIsEmpty_RevokesAllForCallerWithUserRequested()
    {
        var callerGuid = Guid.NewGuid();
        _currentUser.UserId.Returns((Guid?)callerGuid);
        _currentUser.SessionId.Returns((Guid?)Guid.Empty);

        var result = await _sut.Handle(new LogoutOthersCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        await _sessionRepository.Received(1).RevokeAllByUserIdAsync(
            Arg.Is<UserId>(x => x == UserId.From(callerGuid)),
            SessionRevocationReason.UserRequested,
            Arg.Any<CancellationToken>());
        await _sessionRepository.DidNotReceiveWithAnyArgs().RevokeAllExceptAsync(default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenCurrentSessionIdProvided_RevokesAllExceptCurrent()
    {
        var callerGuid = Guid.NewGuid();
        var currentSessionGuid = Guid.NewGuid();
        _currentUser.UserId.Returns((Guid?)callerGuid);
        _currentUser.SessionId.Returns((Guid?)currentSessionGuid);

        var result = await _sut.Handle(new LogoutOthersCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        await _sessionRepository.Received(1).RevokeAllExceptAsync(
            Arg.Is<UserId>(x => x == UserId.From(callerGuid)),
            Arg.Is<SessionId>(x => x == SessionId.From(currentSessionGuid)),
            SessionRevocationReason.UserRequested,
            Arg.Any<CancellationToken>());
        await _sessionRepository.DidNotReceiveWithAnyArgs().RevokeAllByUserIdAsync(default!, default, default);
    }
}
