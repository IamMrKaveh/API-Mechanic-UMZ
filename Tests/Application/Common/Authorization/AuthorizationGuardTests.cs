using Application.Common.Authorization;
using Application.Common.Interfaces;
using Domain.User.ValueObjects;
using SharedKernel.Results;
using Tests.TestInfrastructure.Assertions;

namespace Tests.Application.Common.Authorization;

public class AuthorizationGuardTests : HandlerTestBase
{
    [Fact]
    public void EnsureAuthenticated_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        CurrentUserService.IsAuthenticated.Returns(false);
        CurrentUserService.UserId.Returns((Guid?)null);

        var sut = AuthorizationGuard.EnsureAuthenticated(CurrentUserService);

        sut.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public void EnsureAuthenticated_WhenUserIdNull_ReturnsUnauthorized()
    {
        CurrentUserService.IsAuthenticated.Returns(true);
        CurrentUserService.UserId.Returns((Guid?)null);

        var sut = AuthorizationGuard.EnsureAuthenticated(CurrentUserService);

        sut.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public void EnsureAuthenticated_WhenUserIdEmpty_ReturnsUnauthorized()
    {
        CurrentUserService.IsAuthenticated.Returns(true);
        CurrentUserService.UserId.Returns((Guid?)Guid.Empty);

        var sut = AuthorizationGuard.EnsureAuthenticated(CurrentUserService);

        sut.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public void EnsureAuthenticated_WhenAuthenticatedWithValidUserId_ReturnsSuccess()
    {
        CurrentUserService.IsAuthenticated.Returns(true);
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());

        var sut = AuthorizationGuard.EnsureAuthenticated(CurrentUserService);

        sut.ShouldBeSuccess();
    }

    [Fact]
    public void EnsureOwnerOrAdmin_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        CurrentUserService.IsAuthenticated.Returns(false);
        CurrentUserService.UserId.Returns((Guid?)null);

        var sut = AuthorizationGuard.EnsureOwnerOrAdmin(CurrentUserService, Guid.NewGuid());

        sut.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public void EnsureOwnerOrAdmin_WhenAdmin_ReturnsSuccessRegardlessOfOwner()
    {
        CurrentUserService.IsAuthenticated.Returns(true);
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        CurrentUserService.IsAdmin.Returns(true);

        var sut = AuthorizationGuard.EnsureOwnerOrAdmin(CurrentUserService, Guid.NewGuid());

        sut.ShouldBeSuccess();
    }

    [Fact]
    public void EnsureOwnerOrAdmin_WhenNotAdminAndDifferentOwner_ReturnsForbidden()
    {
        CurrentUserService.IsAuthenticated.Returns(true);
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        CurrentUserService.IsAdmin.Returns(false);

        var sut = AuthorizationGuard.EnsureOwnerOrAdmin(CurrentUserService, Guid.NewGuid());

        sut.ShouldFailWith(ErrorCode.Forbidden);
    }

    [Fact]
    public void EnsureOwnerOrAdmin_WhenNotAdminAndSameOwner_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        CurrentUserService.IsAuthenticated.Returns(true);
        CurrentUserService.UserId.Returns((Guid?)userId);
        CurrentUserService.IsAdmin.Returns(false);

        var sut = AuthorizationGuard.EnsureOwnerOrAdmin(CurrentUserService, userId);

        sut.ShouldBeSuccess();
    }

    [Fact]
    public void EnsureOwnerOrAdmin_WithUserIdOverload_DelegatesToGuidOverload()
    {
        var userId = Guid.NewGuid();
        CurrentUserService.IsAuthenticated.Returns(true);
        CurrentUserService.UserId.Returns((Guid?)userId);
        CurrentUserService.IsAdmin.Returns(false);

        var sut = AuthorizationGuard.EnsureOwnerOrAdmin(CurrentUserService, UserId.From(userId));

        sut.ShouldBeSuccess();
    }

    [Fact]
    public void EnsureAdmin_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        CurrentUserService.IsAuthenticated.Returns(false);
        CurrentUserService.UserId.Returns((Guid?)null);

        var sut = AuthorizationGuard.EnsureAdmin(CurrentUserService);

        sut.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public void EnsureAdmin_WhenAuthenticatedButNotAdmin_ReturnsForbidden()
    {
        CurrentUserService.IsAuthenticated.Returns(true);
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        CurrentUserService.IsAdmin.Returns(false);

        var sut = AuthorizationGuard.EnsureAdmin(CurrentUserService);

        sut.ShouldFailWith(ErrorCode.Forbidden);
    }

    [Fact]
    public void EnsureAdmin_WhenAdmin_ReturnsSuccess()
    {
        CurrentUserService.IsAuthenticated.Returns(true);
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        CurrentUserService.IsAdmin.Returns(true);

        var sut = AuthorizationGuard.EnsureAdmin(CurrentUserService);

        sut.ShouldBeSuccess();
    }

    [Fact]
    public void EnsureAuthenticatedT_WhenNotAuthenticated_ReturnsUnauthorizedFailure()
    {
        CurrentUserService.IsAuthenticated.Returns(false);
        CurrentUserService.UserId.Returns((Guid?)null);

        var sut = AuthorizationGuard.EnsureAuthenticated<string>(CurrentUserService);

        sut.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public void EnsureAuthenticatedT_WhenAuthenticated_ReturnsSuccessWithDefaultValue()
    {
        CurrentUserService.IsAuthenticated.Returns(true);
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());

        var sut = AuthorizationGuard.EnsureAuthenticated<int>(CurrentUserService);

        sut.ShouldBeSuccess();
        sut.Value.ShouldBe(0);
    }

    [Fact]
    public void EnsureOwnerOrAdminT_WhenNotAuthenticated_ReturnsUnauthorizedFailure()
    {
        CurrentUserService.IsAuthenticated.Returns(false);
        CurrentUserService.UserId.Returns((Guid?)null);

        var sut = AuthorizationGuard.EnsureOwnerOrAdmin<string>(CurrentUserService, Guid.NewGuid());

        sut.ShouldFailWith(ErrorCode.Unauthorized);
    }

    [Fact]
    public void EnsureOwnerOrAdminT_WhenAdmin_ReturnsSuccess()
    {
        CurrentUserService.IsAuthenticated.Returns(true);
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        CurrentUserService.IsAdmin.Returns(true);

        var sut = AuthorizationGuard.EnsureOwnerOrAdmin<string>(CurrentUserService, Guid.NewGuid());

        sut.ShouldBeSuccess();
    }

    [Fact]
    public void EnsureOwnerOrAdminT_WhenNotAdminAndDifferentOwner_ReturnsForbidden()
    {
        CurrentUserService.IsAuthenticated.Returns(true);
        CurrentUserService.UserId.Returns((Guid?)Guid.NewGuid());
        CurrentUserService.IsAdmin.Returns(false);

        var sut = AuthorizationGuard.EnsureOwnerOrAdmin<string>(CurrentUserService, Guid.NewGuid());

        sut.ShouldFailWith(ErrorCode.Forbidden);
    }

    [Fact]
    public void EnsureOwnerOrAdminT_WhenNotAdminAndSameOwner_ReturnsSuccess()
    {
        var userId = Guid.NewGuid();
        CurrentUserService.IsAuthenticated.Returns(true);
        CurrentUserService.UserId.Returns((Guid?)userId);
        CurrentUserService.IsAdmin.Returns(false);

        var sut = AuthorizationGuard.EnsureOwnerOrAdmin<string>(CurrentUserService, userId);

        sut.ShouldBeSuccess();
    }
}
