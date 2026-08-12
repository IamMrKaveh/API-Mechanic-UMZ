using Application.Common.Authorization;
using Application.Common.Interfaces;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Common.Authorization;

public class AuthorizationGuardTests
{
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    [Fact]
    public void EnsureAuthenticated_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);

        var sut = AuthorizationGuard.EnsureAuthenticated(_currentUser);

        sut.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public void EnsureAuthenticated_WhenUserIdNull_ReturnsUnauthorized()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)null);

        var sut = AuthorizationGuard.EnsureAuthenticated(_currentUser);

        sut.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public void EnsureAuthenticated_WhenUserIdEmpty_ReturnsUnauthorized()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)Guid.Empty);

        var sut = AuthorizationGuard.EnsureAuthenticated(_currentUser);

        sut.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public void EnsureAuthenticated_WhenAuthenticatedWithValidUserId_ReturnsSuccess()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());

        var sut = AuthorizationGuard.EnsureAuthenticated(_currentUser);

        sut.ShouldBeSuccess();
    }

    [Fact]
    public void EnsureOwnerOrAdmin_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);

        var sut = AuthorizationGuard.EnsureOwnerOrAdmin(_currentUser, Guid.NewGuid());

        sut.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public void EnsureOwnerOrAdmin_WhenAdmin_ReturnsSuccessRegardlessOfOwner()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _currentUser.IsAdmin.Returns(true);

        var sut = AuthorizationGuard.EnsureOwnerOrAdmin(_currentUser, Guid.NewGuid());

        sut.ShouldBeSuccess();
    }

    [Fact]
    public void EnsureOwnerOrAdmin_WhenNotAdminAndDifferentOwner_ReturnsForbidden()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _currentUser.IsAdmin.Returns(false);

        var sut = AuthorizationGuard.EnsureOwnerOrAdmin(_currentUser, Guid.NewGuid());

        sut.ShouldFailWith(ErrorCode.Forbidden);
    }

    [Fact]
    public void EnsureOwnerOrAdmin_WhenNotAdminAndSameOwner_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)userId);
        _currentUser.IsAdmin.Returns(false);

        var sut = AuthorizationGuard.EnsureOwnerOrAdmin(_currentUser, userId);

        sut.ShouldBeSuccess();
    }

    [Fact]
    public void EnsureOwnerOrAdmin_WithUserIdOverload_DelegatesToGuidOverload()
    {
        var userId = Guid.NewGuid();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)userId);
        _currentUser.IsAdmin.Returns(false);

        var sut = AuthorizationGuard.EnsureOwnerOrAdmin(_currentUser, UserId.From(userId));

        sut.ShouldBeSuccess();
    }

    [Fact]
    public void EnsureAdmin_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);

        var sut = AuthorizationGuard.EnsureAdmin(_currentUser);

        sut.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public void EnsureAdmin_WhenAuthenticatedButNotAdmin_ReturnsForbidden()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _currentUser.IsAdmin.Returns(false);

        var sut = AuthorizationGuard.EnsureAdmin(_currentUser);

        sut.ShouldFailWith(ErrorCode.Forbidden);
    }

    [Fact]
    public void EnsureAdmin_WhenAdmin_ReturnsSuccess()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _currentUser.IsAdmin.Returns(true);

        var sut = AuthorizationGuard.EnsureAdmin(_currentUser);

        sut.ShouldBeSuccess();
    }

    [Fact]
    public void EnsureAuthenticatedT_WhenNotAuthenticated_ReturnsUnauthorizedFailure()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);

        var sut = AuthorizationGuard.EnsureAuthenticated<string>(_currentUser);

        sut.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public void EnsureAuthenticatedT_WhenAuthenticated_ReturnsSuccessWithDefaultValue()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());

        var sut = AuthorizationGuard.EnsureAuthenticated<int>(_currentUser);

        sut.ShouldBeSuccess();
        sut.Value.ShouldBe(0);
    }

    [Fact]
    public void EnsureOwnerOrAdminT_WhenNotAuthenticated_ReturnsUnauthorizedFailure()
    {
        _currentUser.IsAuthenticated.Returns(false);
        _currentUser.UserId.Returns((Guid?)null);

        var sut = AuthorizationGuard.EnsureOwnerOrAdmin<string>(_currentUser, Guid.NewGuid());

        sut.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public void EnsureOwnerOrAdminT_WhenAdmin_ReturnsSuccess()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _currentUser.IsAdmin.Returns(true);

        var sut = AuthorizationGuard.EnsureOwnerOrAdmin<string>(_currentUser, Guid.NewGuid());

        sut.ShouldBeSuccess();
    }

    [Fact]
    public void EnsureOwnerOrAdminT_WhenNotAdminAndDifferentOwner_ReturnsForbidden()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)Guid.NewGuid());
        _currentUser.IsAdmin.Returns(false);

        var sut = AuthorizationGuard.EnsureOwnerOrAdmin<string>(_currentUser, Guid.NewGuid());

        sut.ShouldFailWith(ErrorCode.Forbidden);
    }

    [Fact]
    public void EnsureOwnerOrAdminT_WhenNotAdminAndSameOwner_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns((Guid?)userId);
        _currentUser.IsAdmin.Returns(false);

        var sut = AuthorizationGuard.EnsureOwnerOrAdmin<string>(_currentUser, userId);

        sut.ShouldBeSuccess();
    }
}
