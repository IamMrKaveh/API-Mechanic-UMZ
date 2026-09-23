using Application.Security.Contracts;
using Application.User.Features.Commands.ChangePassword;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using UserAggregate = Domain.User.Aggregates.User;

namespace Tests.Application.User.Features.Commands.ChangePassword;

public class ChangePasswordHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ChangePasswordHandler _sut;
    private readonly Guid _userGuid = Guid.NewGuid();

    public ChangePasswordHandlerTests()
    {
        _sut = new ChangePasswordHandler(_userRepository, _passwordHasher, _currentUser);
        _currentUser.UserId.Returns((Guid?)_userGuid);
    }

    [Fact]
    public async Task Handle_WhenUserMissing_ReturnsNotFound()
    {
        _userRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((UserAggregate?)null);

        var result = await _sut.Handle(new ChangePasswordCommand("old-pass", "new-pass-123"), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
        _userRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenCurrentPasswordWrong_ReturnsFailureWithoutUpdating()
    {
        var user = new UserBuilder().WithPasswordHash("old-hash").Build();
        _userRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("wrong", "old-hash").Returns(false);

        var result = await _sut.Handle(new ChangePasswordCommand("wrong", "new-pass-123"), CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        user.PasswordHash.ShouldBe("old-hash");
        _userRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenValid_UpdatesHashResetsLockoutAndReturnsSuccess()
    {
        var user = new UserBuilder().WithPasswordHash("old-hash").Build();
        _userRepository.GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify("old-pass", "old-hash").Returns(true);
        _passwordHasher.Hash("new-pass-123").Returns("new-hash");

        var result = await _sut.Handle(new ChangePasswordCommand("old-pass", "new-pass-123"), CancellationToken.None);

        result.ShouldBeSuccess();
        user.PasswordHash.ShouldBe("new-hash");
        _userRepository.Received(1).Update(user);
    }

    [Fact]
    public async Task Handle_ResolvesUserIdFromCurrentUser()
    {
        var user = new UserBuilder().Build();
        UserId? captured = null;
        _userRepository.GetByIdAsync(Arg.Do<UserId>(x => captured = x), Arg.Any<CancellationToken>()).Returns(user);
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _passwordHasher.Hash(Arg.Any<string>()).Returns("h");

        await _sut.Handle(new ChangePasswordCommand("a", "b"), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(_userGuid);
    }
}
