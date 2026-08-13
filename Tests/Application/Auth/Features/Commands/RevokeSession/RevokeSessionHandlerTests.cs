using Application.Auth.Features.Commands.RevokeSession;
using Application.Common.Interfaces;
using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Auth.Features.Commands.RevokeSession;

public class RevokeSessionHandlerTests
{
    private readonly ISessionRepository _sessionRepository = Substitute.For<ISessionRepository>(); private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>(); private readonly RevokeSessionHandler _sut;

    public RevokeSessionHandlerTests()
    {
        _sut = new RevokeSessionHandler(_sessionRepository, _currentUser);
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ReturnsNotFound()
    {
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _sessionRepository
            .GetByIdAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns((UserSession?)null);

        var result = await _sut.Handle(new RevokeSessionCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _sessionRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenSessionBelongsToAnotherUser_ReturnsForbidden()
    {
        var callerGuid = Guid.NewGuid();
        var ownerUserId = UserId.NewId();
        var session = new UserSessionBuilder().WithUserId(ownerUserId).Build();

        _currentUser.UserId.Returns((Guid?)callerGuid);
        _sessionRepository
            .GetByIdAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await _sut.Handle(new RevokeSessionCommand(session.Id.Value), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        _sessionRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenSessionBelongsToCaller_RevokesWithUserRequestedAndUpdates()
    {
        var callerGuid = Guid.NewGuid();
        var callerUserId = UserId.From(callerGuid);
        var session = new UserSessionBuilder().WithUserId(callerUserId).Build();

        _currentUser.UserId.Returns((Guid?)callerGuid);
        _sessionRepository
            .GetByIdAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(session);

        var result = await _sut.Handle(new RevokeSessionCommand(session.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        session.IsRevoked.ShouldBeTrue();
        session.RevocationReason.ShouldBe(SessionRevocationReason.UserRequested);
        _sessionRepository.Received(1).Update(session);
    }
}
