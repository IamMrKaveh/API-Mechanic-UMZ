using Application.User.Features.Commands.RestoreUser;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Users = Domain.User.Aggregates.User;

namespace Tests.Application.User.Features.Commands.RestoreUser;

public class RestoreUserHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>(); private readonly RestoreUserHandler _sut;

    public RestoreUserHandlerTests()
    {
        _sut = new RestoreUserHandler(_userRepository);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Users?)null);

        var result = await _sut.Handle(new RestoreUserCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _userRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenUserIsDeactivated_ActivatesAndUpdatesRepository()
    {
        var user = new UserBuilder().Build();
        user.Deactivate();

        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.Handle(new RestoreUserCommand(user.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        user.IsActive.ShouldBeTrue();
        _userRepository.Received(1).Update(user);
    }

    [Fact]
    public async Task Handle_WhenUserAlreadyActive_ReturnsSuccessAndUpdatesRepository()
    {
        var user = new UserBuilder().Build();
        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.Handle(new RestoreUserCommand(user.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        user.IsActive.ShouldBeTrue();
        _userRepository.Received(1).Update(user);
    }
}
