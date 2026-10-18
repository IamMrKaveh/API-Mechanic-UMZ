using Application.Common.Interfaces;
using Application.Wallet.Features.Shared;

namespace Tests.Application.Wallet.Features.Shared;

public class WalletUserResolverTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    [Fact]
    public void Resolve_WhenAnonymousCaller_ReturnsOwnId_EvenIfRequestedIdProvided()
    {
        // Non-admin callers must never receive another user's wallet data.
        _currentUser.IsAdmin.Returns(false);
        var ownId = Guid.NewGuid();
        _currentUser.UserId.Returns(ownId);

        var resolved = WalletUserResolver.Resolve(_currentUser, Guid.NewGuid());

        resolved.ShouldBe(ownId);
    }

    [Fact]
    public void Resolve_WhenAdmin_ReturnsRequestedId()
    {
        _currentUser.IsAdmin.Returns(true);
        _currentUser.UserId.Returns(Guid.NewGuid());
        var requested = Guid.NewGuid();

        WalletUserResolver.Resolve(_currentUser, requested).ShouldBe(requested);
    }

    [Fact]
    public void Resolve_WhenAdminWithoutRequestedId_ReturnsOwnId()
    {
        _currentUser.IsAdmin.Returns(true);
        var ownId = Guid.NewGuid();
        _currentUser.UserId.Returns(ownId);

        WalletUserResolver.Resolve(_currentUser, null).ShouldBe(ownId);
        WalletUserResolver.Resolve(_currentUser, Guid.Empty).ShouldBe(ownId);
    }

    [Fact]
    public void Resolve_WhenNoCurrentUser_ReturnsNull()
    {
        _currentUser.IsAdmin.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);

        WalletUserResolver.Resolve(_currentUser, Guid.NewGuid()).ShouldBeNull();
    }

    [Fact]
    public void Resolve_WhenAdminWithoutTargetOrUser_KeepsNullFilter()
    {
        _currentUser.IsAdmin.Returns(true);
        _currentUser.UserId.Returns((Guid?)null);

        WalletUserResolver.Resolve(_currentUser, null).ShouldBeNull();
    }
}
