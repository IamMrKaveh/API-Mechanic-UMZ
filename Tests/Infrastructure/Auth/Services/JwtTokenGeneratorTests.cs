using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Auth.Features.Shared;
using Domain.Security.ValueObjects;
using Domain.User.Aggregates;
using Domain.User.ValueObjects;
using Infrastructure.Auth.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SharedKernel.Constants;
using Tests.TestInfrastructure.Builders;

namespace Tests.Infrastructure.Auth.Services;

public class JwtTokenGeneratorTests
{
    private const string TestKey = "test-signing-key-with-at-least-32-characters-length"; private const string TestIssuer = "mechanic-tests-issuer"; private const string TestAudience = "mechanic-tests-audience"; private const int TestAccessTokenExpirationMinutes = 30;

    private readonly JwtTokenGenerator _sut;

    public JwtTokenGeneratorTests()
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Key = TestKey,
            Issuer = TestIssuer,
            Audience = TestAudience,
            AccessTokenExpirationMinutes = TestAccessTokenExpirationMinutes,
            RefreshTokenExpirationDays = 30,
        });

        _sut = new JwtTokenGenerator(jwtOptions);
    }

    [Fact]
    public void GenerateAccessToken_ForRegularUser_ContainsSubClaimWithUserId()
    {
        var user = BuildUser(isAdmin: false);
        var sessionId = SessionId.NewId();

        var token = _sut.GenerateAccessToken(user, sessionId);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.ShouldContain(c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.Value.ToString());
    }

    [Fact]
    public void GenerateAccessToken_ContainsSessionIdInSidClaim()
    {
        var user = BuildUser(isAdmin: false);
        var sessionId = SessionId.NewId();

        var token = _sut.GenerateAccessToken(user, sessionId);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.ShouldContain(c => c.Type == "sid" && c.Value == sessionId.Value.ToString());
    }

    [Fact]
    public void GenerateAccessToken_ContainsPhoneNumberClaim()
    {
        var phoneNumber = new PhoneNumberBuilder().WithValue("09121234567").Build();
        var user = BuildUser(isAdmin: false, phoneNumber: phoneNumber);

        var token = _sut.GenerateAccessToken(user, SessionId.NewId());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.ShouldContain(c => c.Type == ClaimTypes.MobilePhone && c.Value == "09121234567");
    }

    [Fact]
    public void GenerateAccessToken_ForRegularUser_ContainsUserRoleClaim()
    {
        var user = BuildUser(isAdmin: false);

        var token = _sut.GenerateAccessToken(user, SessionId.NewId());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == AppRoles.User);
    }

    [Fact]
    public void GenerateAccessToken_ForRegularUser_DoesNotContainAdminRoleClaim()
    {
        var user = BuildUser(isAdmin: false);

        var token = _sut.GenerateAccessToken(user, SessionId.NewId());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.ShouldNotContain(c => c.Type == ClaimTypes.Role && c.Value == AppRoles.Admin);
    }

    [Fact]
    public void GenerateAccessToken_ForAdminUser_ContainsBothUserAndAdminRoleClaims()
    {
        var user = BuildUser(isAdmin: true);

        var token = _sut.GenerateAccessToken(user, SessionId.NewId());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == AppRoles.User);
        jwt.Claims.ShouldContain(c => c.Type == ClaimTypes.Role && c.Value == AppRoles.Admin);
    }

    [Fact]
    public void GenerateAccessToken_UsesConfiguredIssuerAndAudience()
    {
        var user = BuildUser(isAdmin: false);

        var token = _sut.GenerateAccessToken(user, SessionId.NewId());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Issuer.ShouldBe(TestIssuer);
        jwt.Audiences.ShouldContain(TestAudience);
    }

    [Fact]
    public void GenerateAccessToken_ExpiresApproximatelyAtConfiguredMinutes()
    {
        var user = BuildUser(isAdmin: false);
        var before = DateTime.UtcNow;

        var token = _sut.GenerateAccessToken(user, SessionId.NewId());

        var after = DateTime.UtcNow;
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.ValidTo.ShouldBeGreaterThanOrEqualTo(before.AddMinutes(TestAccessTokenExpirationMinutes).AddSeconds(-5));
        jwt.ValidTo.ShouldBeLessThanOrEqualTo(after.AddMinutes(TestAccessTokenExpirationMinutes).AddSeconds(5));
    }

    [Fact]
    public void GenerateAccessToken_UsesHmacSha256SigningAlgorithm()
    {
        var user = BuildUser(isAdmin: false);

        var token = _sut.GenerateAccessToken(user, SessionId.NewId());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.SignatureAlgorithm.ShouldBe(SecurityAlgorithms.HmacSha256);
    }

    [Fact]
    public void GenerateAccessToken_EachInvocation_ProducesUniqueJtiClaim()
    {
        var user = BuildUser(isAdmin: false);
        var sessionId = SessionId.NewId();

        var token1 = _sut.GenerateAccessToken(user, sessionId);
        var token2 = _sut.GenerateAccessToken(user, sessionId);

        var jti1 = new JwtSecurityTokenHandler().ReadJwtToken(token1).Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
        var jti2 = new JwtSecurityTokenHandler().ReadJwtToken(token2).Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;

        jti1.ShouldNotBe(jti2);
    }

    [Fact]
    public void GenerateAccessToken_ContainsNameIdClaimWithUserId()
    {
        var user = BuildUser(isAdmin: false);

        var token = _sut.GenerateAccessToken(user, SessionId.NewId());

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Claims.ShouldContain(c => c.Type == "nameid" && c.Value == user.Id.Value.ToString());
    }

    private static User BuildUser(bool isAdmin, PhoneNumber? phoneNumber = null)
    {
        var phone = phoneNumber ?? new PhoneNumberBuilder().Build();
        var user = new UserBuilder()
            .WithPhoneNumber(phone)
            .Build();

        if (isAdmin)
            user.PromoteToAdmin();

        return user;
    }
}
