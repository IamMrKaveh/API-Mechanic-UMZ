using Application.Auth.Features.Shared;
using Application.User.Features.Shared;

namespace Tests.Application.Auth.Features.Shared;

public class AuthDtosTests
{
    private static UserProfileDto SampleUser() => new()
    {
        Id = Guid.NewGuid(),
        PhoneNumber = "09120000000",
        FirstName = "Ali",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    [Fact]
    public void AuthResult_Defaults_AreEmpty()
    {
        var result = new AuthResult();

        result.AccessToken.ShouldBe(string.Empty);
        result.RefreshToken.ShouldBe(string.Empty);
        result.AccessTokenExpiresAt.ShouldBe(default);
        result.RefreshTokenExpiresAt.ShouldBe(default);
        result.User.ShouldBeNull();
        result.IsNewUser.ShouldBeFalse();
    }

    [Fact]
    public void AuthResultResponse_FromAuthResult_MapsAllFields()
    {
        var user = SampleUser();
        var accessExp = DateTime.UtcNow.AddMinutes(15);
        var refreshExp = DateTime.UtcNow.AddDays(7);
        var result = new AuthResult
        {
            AccessToken = "access",
            RefreshToken = "refresh",
            AccessTokenExpiresAt = accessExp,
            RefreshTokenExpiresAt = refreshExp,
            User = user,
            IsNewUser = true
        };

        var response = AuthResultResponse.FromAuthResult(result);

        response.AccessToken.ShouldBe("access");
        response.AccessTokenExpiresAt.ShouldBe(accessExp);
        response.RefreshTokenExpiresAt.ShouldBe(refreshExp);
        response.User.ShouldBe(user);
        response.IsNewUser.ShouldBeTrue();
    }

    [Fact]
    public void AuthResultResponse_FromAuthResult_PreservesIsNewUserFalse()
    {
        var result = new AuthResult
        {
            AccessToken = "a",
            RefreshToken = "r",
            AccessTokenExpiresAt = DateTime.UtcNow,
            RefreshTokenExpiresAt = DateTime.UtcNow,
            User = SampleUser(),
            IsNewUser = false
        };

        AuthResultResponse.FromAuthResult(result).IsNewUser.ShouldBeFalse();
    }

    [Fact]
    public void AuthResultResponse_DoesNotExposeRefreshToken()
    {
        var result = new AuthResult
        {
            AccessToken = "a",
            RefreshToken = "super-secret-refresh",
            AccessTokenExpiresAt = DateTime.UtcNow,
            RefreshTokenExpiresAt = DateTime.UtcNow,
            User = SampleUser()
        };

        var response = AuthResultResponse.FromAuthResult(result);

        response.AccessToken.ShouldBe("a");
        response.ShouldNotBeNull();
    }

    [Fact]
    public void TokenResultDto_StoresTokens()
    {
        var dto = new TokenResultDto("a", "r");

        dto.AccessToken.ShouldBe("a");
        dto.RefreshToken.ShouldBe("r");
    }

    [Fact]
    public void RefreshTokenResult_StoresAllFields()
    {
        var sessionId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var exp = DateTime.UtcNow.AddDays(7);
        var dto = new RefreshTokenResult(sessionId, "rt", exp, userId);

        dto.SessionId.ShouldBe(sessionId);
        dto.RefreshToken.ShouldBe("rt");
        dto.ExpiresAt.ShouldBe(exp);
        dto.UserId.ShouldBe(userId);
    }

    [Fact]
    public void AuthResult_WithExpression_PreservesOtherValues()
    {
        var result = new AuthResult { AccessToken = "a", RefreshToken = "r", User = SampleUser() };

        var updated = result with { IsNewUser = true };

        updated.IsNewUser.ShouldBeTrue();
        updated.AccessToken.ShouldBe("a");
    }
}
