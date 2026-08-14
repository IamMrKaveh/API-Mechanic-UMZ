using Application.Common.Interfaces;
using Application.User.Features.Commands.UpdateProfile;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using SharedKernel.Exceptions;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;
using Tests.TestInfrastructure.Builders;
using Users = Domain.User.Aggregates.User;

namespace Tests.Application.User.Features.Commands.UpdateProfile;

public class UpdateProfileHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>(); private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>(); private readonly UpdateProfileHandler _sut;

    public UpdateProfileHandlerTests()
    {
        _currentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        _sut = new UpdateProfileHandler(_userRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Users?)null);

        var result = await _sut.Handle(new UpdateProfileCommand("Ali", "Ahmadi"), CancellationToken.None);

        result.ShouldFailWith(ErrorCode.NotFound);
        _userRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenUserFound_UpdatesFullNameAndReturnsProfile()
    {
        var user = new UserBuilder()
            .WithFullName(FullName.Create("Old", "Name"))
            .Build();

        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.Handle(new UpdateProfileCommand("Ali", "Ahmadi"), CancellationToken.None);

        result.ShouldBeSuccess();
        user.FullName.FirstName.ShouldBe("Ali");
        user.FullName.LastName.ShouldBe("Ahmadi");
        _userRepository.Received(1).Update(user);
    }

    [Fact]
    public async Task Handle_WhenFirstNameIsNull_KeepsExistingFirstName()
    {
        var user = new UserBuilder()
            .WithFullName(FullName.Create("Existing", "Family"))
            .Build();

        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        var result = await _sut.Handle(new UpdateProfileCommand(null, "NewLast"), CancellationToken.None);

        result.ShouldBeSuccess();
        user.FullName.FirstName.ShouldBe("Existing");
        user.FullName.LastName.ShouldBe("NewLast");
    }

    [Fact]
    public async Task Handle_WhenUserIsInactive_ThrowsDomainException()
    {
        var user = new UserBuilder().Build();
        user.Deactivate();

        _userRepository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(user);

        await Should.ThrowAsync<DomainException>(
            () => _sut.Handle(new UpdateProfileCommand("Ali", "Ahmadi"), CancellationToken.None));

        _userRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }
}
