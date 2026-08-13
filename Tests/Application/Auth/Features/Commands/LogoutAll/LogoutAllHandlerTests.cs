using Application.Auth.Features.Commands.LogoutAll;
using Application.Common.Interfaces;
using Domain.Security.Interfaces;
using Domain.User.ValueObjects;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Auth.Features.Commands.LogoutAll;

public class LogoutAllHandlerTests
{
    private readonly ISessionRepository _sessionRepository = Substitute.For<ISessionRepository>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly LogoutAllHandler _sut;

    public LogoutAllHandlerTests()
    {
        _sut = new LogoutAllHandler(_sessionRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenTargetUserIdProvided_RevokesAllForTargetUser()
    {
        var targetGuid = Guid.NewGuid();
        var callerGuid = Guid.NewGuid();
        _currentUser.UserId.Returns((Guid?)callerGuid);

        var result = await _sut.Handle(new LogoutAllCommand(targetGuid), CancellationToken.None);

        result.ShouldBeSuccess();
        await _sessionRepository.Received(1).RevokeAllByUserIdAsync(
            Arg.Is<UserId>(x => x == UserId.From(targetGuid)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTargetUserIdNullAndCallerAuthenticated_RevokesAllForCaller()
    {
        var callerGuid = Guid.NewGuid();
        _currentUser.UserId.Returns((Guid?)callerGuid);

        var result = await _sut.Handle(new LogoutAllCommand(null), CancellationToken.None);

        result.ShouldBeSuccess();
        await _sessionRepository.Received(1).RevokeAllByUserIdAsync(
            Arg.Is<UserId>(x => x == UserId.From(callerGuid)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTargetAndCallerAreNull_ThrowsInvalidOperationException()
    {
        _currentUser.UserId.Returns((Guid?)null);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            _sut.Handle(new LogoutAllCommand(null), CancellationToken.None));

        await _sessionRepository.DidNotReceiveWithAnyArgs().RevokeAllByUserIdAsync(default!, default);
    }
}
