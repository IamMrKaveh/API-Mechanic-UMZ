using Application.Audit.Contracts;
using Application.Auth.Contracts;
using Application.Common.Interfaces;
using Application.User.Features.Commands.DeactivateAccount;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using SharedKernel.ValueObjects;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Users = Domain.User.Aggregates.User;

namespace Tests.Application.User.Features.Commands.DeactivateAccount;

public class DeactivateAccountHandlerTests : HandlerTestBase
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>(); private readonly ISessionService _sessionService = Substitute.For<ISessionService>(); private readonly DeactivateAccountHandler _sut;

    public DeactivateAccountHandlerTests()
    {
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _sut = new DeactivateAccountHandler(
            _userRepository,
            _sessionService,
            CurrentUserService,
            AuditService);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Users?)null);

        var result = await _sut.Handle(new DeactivateAccountCommand(), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _userRepository.DidNotReceiveWithAnyArgs().Update(default!);
        await _sessionService.DidNotReceiveWithAnyArgs().RevokeAllSessionsAsync(default!, default);
        await AuditService.DidNotReceiveWithAnyArgs().LogSecurityEventAsync(
            default!, default!, default!, default, default);
    }

    [Fact]
    public async Task Handle_WhenUserFound_DeactivatesRevokesSessionsAndLogsSecurityEvent()
    {
        var user = new UserBuilder().Build();

        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.Handle(new DeactivateAccountCommand(), CancellationToken.None);

        result.ShouldBeSuccess();
        user.IsActive.ShouldBeFalse();
        _userRepository.Received(1).Update(user);
        await _sessionService.Received(1).RevokeAllSessionsAsync(
            Arg.Is<UserId>(x => x == user.Id),
            Arg.Any<CancellationToken>());
        await AuditService.Received(1).LogSecurityEventAsync(
            "AccountDeactivated",
            Arg.Any<string>(),
            Arg.Is<IpAddress>(x => x == IpAddress.Unknown),
            Arg.Is<UserId?>(x => x != null && x == user.Id),
            Arg.Any<CancellationToken>());
    }
}
