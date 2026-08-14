using Application.User.Features.Commands.ChangeUserStatus;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Users = Domain.User.Aggregates.User;

namespace Tests.Application.User.Features.Commands.ChangeUserStatus;

public class ChangeUserStatusHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>(); private readonly ChangeUserStatusHandler _sut;

    public ChangeUserStatusHandlerTests()
    {
        _sut = new ChangeUserStatusHandler(_userRepository);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Users?)null);

        var result = await _sut.Handle(
            new ChangeUserStatusCommand(Guid.NewGuid(), true),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _userRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenIsActiveTrueAndUserIsInactive_ActivatesAndUpdates()
    {
        var user = new UserBuilder().Build();
        user.Deactivate();

        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.Handle(
            new ChangeUserStatusCommand(user.Id.Value, true),
            CancellationToken.None);

        result.ShouldBeSuccess();
        user.IsActive.ShouldBeTrue();
        _userRepository.Received(1).Update(user);
    }

    [Fact]
    public async Task Handle_WhenIsActiveFalseAndUserIsActive_DeactivatesAndUpdates()
    {
        var user = new UserBuilder().Build();

        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.Handle(
            new ChangeUserStatusCommand(user.Id.Value, false),
            CancellationToken.None);

        result.ShouldBeSuccess();
        user.IsActive.ShouldBeFalse();
        _userRepository.Received(1).Update(user);
    }
}
