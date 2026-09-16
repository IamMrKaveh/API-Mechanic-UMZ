using Application.Auth.Contracts;
using Application.Auth.Features.Commands.Logout;
using Application.Auth.Features.Commands.LogoutAll;
using Application.Auth.Features.Commands.RefreshToken;
using Application.Auth.Features.Commands.VerifyOtp;
using Application.Auth.Features.Shared;
using Application.User.Features.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Presentation.Auth.Endpoints;
using Presentation.Auth.Requests;
using Presentation.Base.Responses;
using Presentation.Common.Cookies;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Common.Options;

namespace Tests.Presentation.Auth.Endpoints;

public class AuthControllerTests
{
    private const string CookieName = "refresh_token";

    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        var cookieOptions = Options.Create(new AuthCookieOptions
        {
            RefreshTokenName = CookieName,
            Path = "/",
            SameSite = "Strict",
            Secure = true,
            HttpOnly = true,
            Domain = null
        });

        _controller = new AuthController(
            _mediator,
            Substitute.For<IMapper>(),
            Substitute.For<IGoogleAuthenticationService>(),
            new AuthCookieService(cookieOptions));

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task RefreshToken_MissingCookie_ReturnsUnauthorized_AndSkipsMediator()
    {
        var result = await _controller.RefreshToken(CancellationToken.None);

        var unauthorized = result.ShouldBeOfType<UnauthorizedObjectResult>();
        unauthorized.StatusCode.ShouldBe(StatusCodes.Status401Unauthorized);
        var body = unauthorized.Value.ShouldBeOfType<ApiResponse>();
        body.Success.ShouldBeFalse();
        _mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<IRequest<ServiceResult>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshToken_WhitespaceCookie_ReturnsUnauthorized()
    {
        SetRequestCookie("   ");

        var result = await _controller.RefreshToken(CancellationToken.None);

        result.ShouldBeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task RefreshToken_WithCookie_SendsCommandWithCookieToken()
    {
        SetRequestCookie("old-refresh");
        StubRefreshSuccess();

        await _controller.RefreshToken(CancellationToken.None);

        await _mediator.Received(1).Send(
            Arg.Is<RefreshTokenCommand>(command => command.RefreshToken == "old-refresh"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshToken_Success_Returns201_WithResponseDto_AndNoRefreshTokenInBody()
    {
        SetRequestCookie("old-refresh");
        StubRefreshSuccess();

        var result = await _controller.RefreshToken(CancellationToken.None);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status201Created);
        var body = objectResult.Value.ShouldBeOfType<ApiResponse<AuthResultResponse>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldNotBeNull();
        body.Data!.AccessToken.ShouldBe("new-access");
        body.Data.RefreshTokenExpiresAt.ShouldBe(DateTime.UnixEpoch);
        typeof(AuthResultResponse).GetProperty("RefreshToken").ShouldBeNull();
    }

    [Fact]
    public async Task RefreshToken_Success_WritesRefreshCookie()
    {
        SetRequestCookie("old-refresh");
        StubRefreshSuccess();

        await _controller.RefreshToken(CancellationToken.None);

        var setCookie = _controller.Response.Headers.SetCookie.ToString();
        setCookie.ShouldContain($"{CookieName}=new-refresh");
        setCookie.ShouldContain("path=/");
        setCookie.ShouldContain("secure");
        setCookie.ShouldContain("httponly");
        setCookie.ShouldContain("samesite=strict");
    }

    [Fact]
    public async Task RefreshToken_Failure_DoesNotWriteCookie_AndMapsFailure()
    {
        SetRequestCookie("expired-refresh");
        _mediator
            .Send(Arg.Any<RefreshTokenCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ServiceResult<AuthResult>.Unauthorized("نشست منقضی شده است.")));

        var result = await _controller.RefreshToken(CancellationToken.None);

        result.ShouldBeOfType<ObjectResult>();
        _controller.Response.Headers.ContainsKey("Set-Cookie").ShouldBeFalse();
    }

    [Fact]
    public async Task Logout_PassesCookieTokenToCommand_AndReturnsOk()
    {
        SetRequestCookie("old-refresh");
        _mediator
            .Send(Arg.Any<LogoutCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ServiceResult.Success()));

        var result = await _controller.Logout(CancellationToken.None);

        result.ShouldBeOfType<OkObjectResult>();
        await _mediator.Received(1).Send(
            Arg.Is<LogoutCommand>(command => command.RefreshToken == "old-refresh"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Logout_ClearsCookie_EvenWhenTokenMissing()
    {
        _mediator
            .Send(Arg.Any<LogoutCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ServiceResult.Success()));

        var result = await _controller.Logout(CancellationToken.None);

        result.ShouldBeOfType<OkObjectResult>();
        await _mediator.Received(1).Send(
            Arg.Is<LogoutCommand>(command => command.RefreshToken == null),
            Arg.Any<CancellationToken>());
        var setCookie = _controller.Response.Headers.SetCookie.ToString();
        setCookie.ShouldContain($"{CookieName}=");
        setCookie.ShouldContain("expires=Thu, 01 Jan 1970 00:00:00 GMT");
    }

    [Fact]
    public async Task LogoutAll_ClearsCookie()
    {
        _mediator
            .Send(Arg.Any<LogoutAllCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ServiceResult.Success()));

        var result = await _controller.LogoutAll(CancellationToken.None);

        result.ShouldBeOfType<OkObjectResult>();
        var setCookie = _controller.Response.Headers.SetCookie.ToString();
        setCookie.ShouldContain($"{CookieName}=");
        setCookie.ShouldContain("expires=Thu, 01 Jan 1970 00:00:00 GMT");
    }

    [Fact]
    public async Task VerifyOtp_Success_Returns201_WithCookie_AndNoRefreshTokenInBody()
    {
        StubRefreshSuccess();
        var request = new VerifyOtpRequest("09123456789", "123456");

        var result = await _controller.VerifyOtp(request, CancellationToken.None);

        var objectResult = result.ShouldBeOfType<ObjectResult>();
        objectResult.StatusCode.ShouldBe(StatusCodes.Status201Created);
        var body = objectResult.Value.ShouldBeOfType<ApiResponse<AuthResultResponse>>();
        body.Data!.IsNewUser.ShouldBeFalse();
        _controller.Response.Headers.SetCookie.ToString().ShouldContain($"{CookieName}=new-refresh");
    }

    [Fact]
    public void AuthController_UnsafeActions_HaveValidateAntiForgeryToken()
    {
        var unsafeMethods = new[]
        {
            typeof(AuthController).GetMethod(nameof(AuthController.RefreshToken)),
            typeof(AuthController).GetMethod(nameof(AuthController.Logout)),
            typeof(AuthController).GetMethod(nameof(AuthController.LogoutAll))
        };

        foreach (var method in unsafeMethods)
        {
            method.ShouldNotBeNull();
            method!.GetCustomAttributes(true)
                .Any(attribute => attribute is ValidateAntiForgeryTokenAttribute)
                .ShouldBeTrue();
        }
    }

    private void SetRequestCookie(string value)
    {
        _controller.ControllerContext.HttpContext.Request.Headers.Cookie = $"{CookieName}={value}";
    }

    private void StubRefreshSuccess()
    {
        _mediator
            .Send(Arg.Any<RefreshTokenCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ServiceResult<AuthResult>.Success(new AuthResult
            {
                AccessToken = "new-access",
                RefreshToken = "new-refresh",
                AccessTokenExpiresAt = DateTime.UnixEpoch,
                RefreshTokenExpiresAt = DateTime.UnixEpoch,
                User = new UserProfileDto { PhoneNumber = "09123456789" },
                IsNewUser = false
            })));
    }
}
