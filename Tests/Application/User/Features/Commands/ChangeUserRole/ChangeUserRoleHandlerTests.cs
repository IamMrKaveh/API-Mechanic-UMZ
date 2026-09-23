using Application.User.Features.Commands.ChangeUserRole;
using Domain.User.Interfaces;
using Domain.User.ValueObjects;
using UserAggregate = Domain.User.Aggregates.User;

namespace Tests.Application.User.Features.Commands.ChangeUserRole;

public class ChangeUserRoleHandlerTests
{
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly ChangeUserRoleHandler _sut;
    private readonly Guid _adminGuid = Guid.NewGuid();

    public ChangeUserRoleHandlerTests()
    {
        _sut = new ChangeUserRoleHandler(_userRepository, _currentUser);
        _currentUser.UserId.Returns((Guid?)_adminGuid);
    }

    [Fact]
    public async Task Handle_WhenUserMissing_ReturnsNotFound()
    {
        _userRepository.GetActiveByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((UserAggregate?)null);

        var result = await _sut.Handle(new ChangeUserRoleCommand(Guid.NewGuid(), true), CancellationToken.None);

        result.ShouldFailWithType(ErrorType.NotFound);
        _userRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenChangingOwnRole_ReturnsForbidden()
    {
        var admin = new UserBuilder().Build();
        admin.PromoteToAdmin();
        _userRepository.GetActiveByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(admin);
        _currentUser.UserId.Returns((Guid?)admin.Id.Value);

        var result = await _sut.Handle(new ChangeUserRoleCommand(admin.Id.Value, false), CancellationToken.None);

        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        _userRepository.DidNotReceiveWithAnyArgs().Update(default!);
    }

    [Fact]
    public async Task Handle_WhenPromoting_PromotesAndUpdates()
    {
        var user = new UserBuilder().Build();
        user.IsAdmin.ShouldBeFalse();
        _userRepository.GetActiveByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(new ChangeUserRoleCommand(user.Id.Value, true), CancellationToken.None);

        result.ShouldBeSuccess();
        user.IsAdmin.ShouldBeTrue();
        _userRepository.Received(1).Update(user);
    }

    [Fact]
    public async Task Handle_WhenDemoting_DemotesAndUpdates()
    {
        var user = new UserBuilder().Build();
        user.PromoteToAdmin();
        _userRepository.GetActiveByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(new ChangeUserRoleCommand(user.Id.Value, false), CancellationToken.None);

        result.ShouldBeSuccess();
        user.IsAdmin.ShouldBeFalse();
        _userRepository.Received(1).Update(user);
    }

    [Fact]
    public async Task Handle_ResolvesTargetIdFromRequest()
    {
        var user = new UserBuilder().Build();
        UserId? captured = null;
        _userRepository.GetActiveByIdAsync(Arg.Do<UserId>(x => captured = x), Arg.Any<CancellationToken>()).Returns(user);

        await _sut.Handle(new ChangeUserRoleCommand(user.Id.Value, true), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!.Value.ShouldBe(user.Id.Value);
    }
}
