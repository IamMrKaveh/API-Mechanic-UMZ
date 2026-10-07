using Application.Auth.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Presentation.Auth.Services;
using System.Security.Claims;

namespace Tests.Presentation.Auth.Services;

public class HttpGoogleAuthenticationServiceTests
{
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly HttpGoogleAuthenticationService _sut;

    public HttpGoogleAuthenticationServiceTests()
    {
        _sut = new HttpGoogleAuthenticationService(_httpContextAccessor);
    }

    [Fact]
    public async Task AuthenticateAsync_WhenNotSucceeded_ReturnsNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _httpContextAccessor.HttpContext.Returns(httpContext);
        httpContext.RequestServices = Substitute.For<IServiceProvider>();
        httpContext.RequestServices
            .GetService(typeof(IAuthenticationService))
            .Returns(new FailingAuthenticationService());

        // Act
        var result = await _sut.AuthenticateAsync(CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_WhenEmailOrProviderKeyMissing_ReturnsNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _httpContextAccessor.HttpContext.Returns(httpContext);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.GivenName, "First"),
            new Claim(ClaimTypes.Surname, "Last")
        ]));
        var ticket = new AuthenticationTicket(principal, GoogleDefaults.AuthenticationScheme);
        httpContext.RequestServices = Substitute.For<IServiceProvider>();
        httpContext.RequestServices
            .GetService(typeof(IAuthenticationService))
            .Returns(new SucceedingAuthenticationService(ticket));

        // Act
        var result = await _sut.AuthenticateAsync(CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidClaims_ReturnsProfile()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _httpContextAccessor.HttpContext.Returns(httpContext);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim(ClaimTypes.GivenName, "First"),
            new Claim(ClaimTypes.Surname, "Last"),
            new Claim(ClaimTypes.NameIdentifier, "google-key-1")
        ]));
        var ticket = new AuthenticationTicket(principal, GoogleDefaults.AuthenticationScheme);
        httpContext.RequestServices = Substitute.For<IServiceProvider>();
        httpContext.RequestServices
            .GetService(typeof(IAuthenticationService))
            .Returns(new SucceedingAuthenticationService(ticket));

        // Act
        var result = await _sut.AuthenticateAsync(CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result!.Email.ShouldBe("user@example.com");
        result.FirstName.ShouldBe("First");
        result.LastName.ShouldBe("Last");
        result.ProviderKey.ShouldBe("google-key-1");
    }

    [Fact]
    public async Task AuthenticateAsync_WithMissingNames_DefaultsToEmptyString()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _httpContextAccessor.HttpContext.Returns(httpContext);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim(ClaimTypes.NameIdentifier, "google-key-1")
        ]));
        var ticket = new AuthenticationTicket(principal, GoogleDefaults.AuthenticationScheme);
        httpContext.RequestServices = Substitute.For<IServiceProvider>();
        httpContext.RequestServices
            .GetService(typeof(IAuthenticationService))
            .Returns(new SucceedingAuthenticationService(ticket));

        // Act
        var result = await _sut.AuthenticateAsync(CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result!.FirstName.ShouldBe(string.Empty);
        result.LastName.ShouldBe(string.Empty);
    }

    private sealed class FailingAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.Fail("denied"));

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;
    }

    private sealed class SucceedingAuthenticationService(AuthenticationTicket ticket) : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.Success(ticket));

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;
    }
}
