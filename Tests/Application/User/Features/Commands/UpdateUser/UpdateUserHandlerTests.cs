using Application.User.Features.Commands.UpdateUser;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Users = Domain.User.Aggregates.User;

namespace Tests.Application.User.Features.Commands.UpdateUser;

public class UpdateUserHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>(); private readonly UpdateUserHandler _sut;

    public UpdateUserHandlerTests()
    {
        _sut = new UpdateUserHandler(_userRepository);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Users?)null);

        var result = await _sut.Handle(
            new UpdateUserCommand(Guid.NewGuid(), "Ali", "Ahmadi"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _userRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenUserIsInactive_ReturnsForbidden()
    {
        var user = new UserBuilder().Build();
        user.Deactivate();

        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.Handle(
            new UpdateUserCommand(user.Id.Value, "Ali", "Ahmadi"),
            CancellationToken.None);

        result.ShouldFailWith(ErrorCode.Forbidden);
        _userRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenActiveUser_UpdatesFullNameAndCallsRepositoryUpdate()
    {
        var user = new UserBuilder()
            .WithFullName(FullName.Create("Old", "Name"))
            .Build();

        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.Handle(
            new UpdateUserCommand(user.Id.Value, "Ali", "Ahmadi"),
            CancellationToken.None);

        result.ShouldBeSuccess();
        user.FullName.FirstName.ShouldBe("Ali");
        user.FullName.LastName.ShouldBe("Ahmadi");
        _userRepository.Received(1).Update(user);
    }

    [Fact]
    public async Task Handle_WhenFirstNameEmpty_KeepsExistingFirstName()
    {
        var user = new UserBuilder()
            .WithFullName(FullName.Create("Existing", "Family"))
            .Build();

        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.Handle(
            new UpdateUserCommand(user.Id.Value, string.Empty, "NewLast"),
            CancellationToken.None);

        result.ShouldBeSuccess();
        user.FullName.FirstName.ShouldBe("Existing");
        user.FullName.LastName.ShouldBe("NewLast");
    }

    [Fact]
    public async Task Handle_WhenLastNameEmpty_KeepsExistingLastName()
    {
        var user = new UserBuilder()
            .WithFullName(FullName.Create("Existing", "Family"))
            .Build();

        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.Handle(
            new UpdateUserCommand(user.Id.Value, "NewFirst", string.Empty),
            CancellationToken.None);

        result.ShouldBeSuccess();
        user.FullName.FirstName.ShouldBe("NewFirst");
        user.FullName.LastName.ShouldBe("Family");
    }
}
