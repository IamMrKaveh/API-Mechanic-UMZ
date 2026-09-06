using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Presentation.Common.Services;
using SharedKernel.Constants;

namespace Tests.Presentation.Common.Services;

public class CurrentUserServiceTests
{
    private static (CurrentUserService Sut, DefaultHttpContext Context) BuildSut(
        Dictionary<string, string?>? config = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();
        var accessor = Substitute.For<IHttpContextAccessor>();
        var context = new DefaultHttpContext();
        accessor.HttpContext.Returns(context);
        return (new CurrentUserService(accessor, configuration), context);
    }

    private static ClaimsPrincipal AuthenticatedPrincipal(
        Guid? userId = null,
        Guid? sessionId = null,
        bool admin = false)
    {
        var claims = new List<Claim>();
        if (userId.HasValue)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
        if (sessionId.HasValue)
            claims.Add(new Claim("sid", sessionId.Value.ToString()));
        if (admin)
            claims.Add(new Claim(ClaimTypes.Role, AppRoles.Admin));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "mock"));
    }

    [Fact]
    public void IsAuthenticated_WithoutContext_IsFalse()
    {
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns((HttpContext?)null);
        var config = new ConfigurationBuilder().Build();
        var sut = new CurrentUserService(accessor, config);

        sut.IsAuthenticated.ShouldBeFalse();
        sut.UserId.ShouldBeNull();
        sut.SessionId.ShouldBeNull();
        sut.IsAdmin.ShouldBeFalse();
        sut.IpAddress.ShouldBeNull();
        sut.UserAgent.ShouldBeNull();
        sut.GuestToken.ShouldBeNull();
    }

    [Fact]
    public void UserId_WithNameIdentifierClaim_ReturnsGuid()
    {
        var (sut, context) = BuildSut();
        var id = Guid.NewGuid();
        context.User = AuthenticatedPrincipal(userId: id);

        sut.IsAuthenticated.ShouldBeTrue();
        sut.UserId.ShouldBe(id);
    }

    [Theory]
    [InlineData("sub")]
    [InlineData("nameid")]
    public void UserId_FallsBackToSubAndNameIdClaims(string claimType)
    {
        var (sut, context) = BuildSut();
        var id = Guid.NewGuid();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(claimType, id.ToString())], "mock"));

        sut.UserId.ShouldBe(id);
    }

    [Fact]
    public void UserId_WithNonGuidValue_ReturnsNull()
    {
        var (sut, context) = BuildSut();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "not-a-guid")], "mock"));

        sut.UserId.ShouldBeNull();
    }

    [Fact]
    public void SessionId_WithSidClaim_ReturnsGuid()
    {
        var (sut, context) = BuildSut();
        var sid = Guid.NewGuid();
        context.User = AuthenticatedPrincipal(userId: Guid.NewGuid(), sessionId: sid);

        sut.SessionId.ShouldBe(sid);
    }

    [Fact]
    public void SessionId_WithoutClaim_ReturnsNull()
    {
        var (sut, context) = BuildSut();
        context.User = AuthenticatedPrincipal(userId: Guid.NewGuid());

        sut.SessionId.ShouldBeNull();
    }

    [Fact]
    public void IsAdmin_WithAdminRoleClaim_IsTrue()
    {
        var (sut, context) = BuildSut();
        context.User = AuthenticatedPrincipal(userId: Guid.NewGuid(), admin: true);

        sut.IsAdmin.ShouldBeTrue();
    }

    [Fact]
    public void IsAdmin_WithPlainRoleClaim_IsTrue()
    {
        var (sut, context) = BuildSut();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("role", AppRoles.Admin)], "mock"));

        sut.IsAdmin.ShouldBeTrue();
    }

    [Fact]
    public void IsAdmin_WithoutAdminRole_IsFalse()
    {
        var (sut, context) = BuildSut();
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, AppRoles.User)], "mock"));

        sut.IsAdmin.ShouldBeFalse();
    }

    [Fact]
    public void IpAddress_ReturnsRemoteIp()
    {
        var (sut, context) = BuildSut();
        context.Connection.RemoteIpAddress = IPAddress.Parse("9.8.7.6");

        sut.IpAddress.ShouldBe("9.8.7.6");
    }

    [Fact]
    public void UserAgent_ReturnsHeaderValue()
    {
        var (sut, context) = BuildSut();
        context.Request.Headers.UserAgent = "TestAgent/1.0";

        sut.UserAgent.ShouldBe("TestAgent/1.0");
    }

    [Fact]
    public void GuestToken_ReturnsHeaderValue()
    {
        var (sut, context) = BuildSut();
        context.Request.Headers["X-Guest-Token"] = "guest-abc";

        sut.GuestToken.ShouldBe("guest-abc");
    }

    [Fact]
    public void FrontendBaseUrl_WithAllowedOrigin_ReturnsOrigin()
    {
        var (sut, context) = BuildSut(new Dictionary<string, string?>
        {
            ["Security:AllowedOrigins:0"] = "https://app.example.com",
            ["FrontendUrls:BaseUrl"] = "https://fallback.example.com"
        });
        context.Request.Headers.Origin = "https://app.example.com/";

        sut.FrontendBaseUrl.ShouldBe("https://app.example.com");
    }

    [Fact]
    public void FrontendBaseUrl_WithDisallowedOrigin_ReturnsConfiguredFallback()
    {
        var (sut, context) = BuildSut(new Dictionary<string, string?>
        {
            ["Security:AllowedOrigins:0"] = "https://app.example.com",
            ["FrontendUrls:BaseUrl"] = "https://fallback.example.com/"
        });
        context.Request.Headers.Origin = "https://evil.example.com";

        sut.FrontendBaseUrl.ShouldBe("https://fallback.example.com");
    }

    [Fact]
    public void FrontendBaseUrl_WithoutAnyConfig_ReturnsDefault()
    {
        var (sut, _) = BuildSut();

        sut.FrontendBaseUrl.ShouldBe("https://ledka-co.ir");
    }
}
