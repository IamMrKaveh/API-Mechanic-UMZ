using Application.Auth.Features.Commands.AdminRevokeSession;
using Domain.Security.Aggregates;
using Domain.Security.Enums;
using Domain.Security.Interfaces;
using Domain.Security.ValueObjects;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;

namespace Tests.Application.Auth.Features.Commands.AdminRevokeSession;

public class AdminRevokeSessionHandlerTests
{
    private readonly ISessionRepository _sessionRepository = Substitute.For<ISessionRepository>(); private readonly AdminRevokeSessionHandler _sut;

    public AdminRevokeSessionHandlerTests()
    {
        _sut = new AdminRevokeSessionHandler(_sessionRepository);
    }

    [Fact]
    public async Task Handle_WhenSessionNotFound_ReturnsNotFound()
    {
        _sessionRepository
            .GetByIdAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns((UserSession?)null);

        var command = new AdminRevokeSessionCommand(Guid.NewGuid(), Guid.NewGuid());

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _sessionRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenSessionBelongsToAnotherUser_ReturnsNotFound()
    {
        var targetUserGuid = Guid.NewGuid();
        var ownerUserId = UserId.NewId();
        var session = new UserSessionBuilder().WithUserId(ownerUserId).Build();

        _sessionRepository
            .GetByIdAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(session);

        var command = new AdminRevokeSessionCommand(targetUserGuid, session.Id.Value);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _sessionRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenSessionMatchesTargetUser_RevokesAndUpdates()
    {
        var targetUserGuid = Guid.NewGuid();
        var userId = UserId.From(targetUserGuid);
        var session = new UserSessionBuilder().WithUserId(userId).Build();

        _sessionRepository
            .GetByIdAsync(Arg.Any<SessionId>(), Arg.Any<CancellationToken>())
            .Returns(session);

        var command = new AdminRevokeSessionCommand(targetUserGuid, session.Id.Value);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.ShouldBeSuccess();
        session.IsRevoked.ShouldBeTrue();
        session.RevocationReason.ShouldBe(SessionRevocationReason.AdminRevoked);
        _sessionRepository.Received(1).Update(session);
    }
}
