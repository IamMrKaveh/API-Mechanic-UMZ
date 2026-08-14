using Application.Common.Interfaces;
using Application.User.Features.Commands.DeleteUser;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Users = Domain.User.Aggregates.User;

namespace Tests.Application.User.Features.Commands.DeleteUser;

public class DeleteUserHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly DeleteUserHandler _sut;

    public DeleteUserHandlerTests()
    {
        _sut = new DeleteUserHandler(_userRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenTargetIsCurrentUser_ReturnsForbidden()
    {
        var currentGuid = Guid.NewGuid();
        _currentUserService.UserId.Returns((Guid?)currentGuid);

        var result = await _sut.Handle(new DeleteUserCommand(currentGuid), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        await _userRepository.DidNotReceiveWithAnyArgs().GetByIdAsync(default!);
        _userRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _userRepository
            .GetByIdAsync(Arg.Any<UserId>())
            .Returns((Users?)null);

        var result = await _sut.Handle(new DeleteUserCommand(Guid.NewGuid()), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _userRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenUserExists_DeactivatesAndUpdatesRepository()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        var user = new UserBuilder().Build();
        _userRepository
            .GetByIdAsync(Arg.Any<UserId>())
            .Returns(user);

        var result = await _sut.Handle(new DeleteUserCommand(user.Id.Value), CancellationToken.None);

        result.ShouldBeSuccess();
        user.IsActive.ShouldBeFalse();
        _userRepository.Received(1).Update(user);
    }
}
