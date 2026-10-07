using Application.Auth.Contracts;
using Application.Auth.Features.Commands.GoogleLogin;
using Application.Auth.Features.Commands.Logout;
using Application.Auth.Features.Commands.LogoutAll;
using Application.Auth.Features.Commands.RefreshToken;
using Application.Auth.Features.Commands.SendOtp;
using Application.Auth.Features.Commands.VerifyOtp;
using Application.Auth.Features.Shared;
using Application.User.Features.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
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
    private readonly IGoogleAuthenticationService _googleAuthService = Substitute.For<IGoogleAuthenticationService>();
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
            _googleAuthService,
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
        await _mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<IRequest<ServiceResult>>(), Arg.Any<CancellationToken>());
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
        StubVerifyOtpSuccess();
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

    [Fact]
    public void GoogleLogin_ReturnsChallengeResult()
    {
        // Arrange
        var urlHelper = Substitute.For<IUrlHelper>();
        urlHelper.Action(Arg.Any<UrlActionContext>()).Returns("/api/v1/auth/google/callback");
        _controller.Url = urlHelper;

        // Act
        var result = _controller.GoogleLogin();

        // Assert
        result.ShouldBeOfType<ChallengeResult>();
    }

    [Fact]
    public async Task GoogleCallback_WhenProfileIsNull_ReturnsBadRequest()
    {
        // Arrange
        _googleAuthService.AuthenticateAsync(Arg.Any<CancellationToken>()).Returns((GoogleProfile?)null);

        // Act
        var result = await _controller.GoogleCallback(CancellationToken.None);

        // Assert
        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        await _mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<GoogleLoginCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GoogleCallback_WithValidProfile_SendsCommand_AndReturnsOkWithCookie()
    {
        // Arrange
        _googleAuthService.AuthenticateAsync(Arg.Any<CancellationToken>())
            .Returns(new GoogleProfile("user@example.com", "First", "Last", "google-key-1"));
        _mediator
            .Send(Arg.Any<GoogleLoginCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ServiceResult<TokenResultDto>.Success(new TokenResultDto("new-access", "new-refresh"))));

        // Act
        var result = await _controller.GoogleCallback(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse>();
        body.Success.ShouldBeTrue();
        await _mediator.Received(1).Send(
            Arg.Is<GoogleLoginCommand>(c => c.Email == "user@example.com" && c.ProviderKey == "google-key-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestOtp_WithValidRequest_SendsCommand_AndReturns201()
    {
        // Arrange
        var request = new SendOtpRequest("09123456789");
        _mediator
            .Send(Arg.Any<SendOtpCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ServiceResult.Success()));

        // Act
        var result = await _controller.RequestOtp(request, CancellationToken.None);

        // Assert
        var created = result.ShouldBeOfType<ObjectResult>();
        created.StatusCode.ShouldBe(StatusCodes.Status201Created);
        await _mediator.Received(1).Send(
            Arg.Is<SendOtpCommand>(c => c.PhoneNumber == "09123456789"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestOtp_WhenFailure_MapsToErrorStatus()
    {
        // Arrange
        var request = new SendOtpRequest("09123456789");
        _mediator
            .Send(Arg.Any<SendOtpCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ServiceResult.Failure("خطا")));

        // Act
        var result = await _controller.RequestOtp(request, CancellationToken.None);

        // Assert
        var failure = result.ShouldBeOfType<ObjectResult>();
        failure.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
        var body = failure.Value.ShouldBeOfType<ApiResponse>();
        body.Success.ShouldBeFalse();
    }

    [Fact]
    public void AuthController_HasRouteAttribute()
    {
        var routeAttr = typeof(AuthController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/auth");
    }

    [Theory]
    [InlineData(nameof(AuthController.GoogleLogin), "google")]
    [InlineData(nameof(AuthController.GoogleCallback), "google/callback")]
    [InlineData(nameof(AuthController.RequestOtp), "otp")]
    [InlineData(nameof(AuthController.VerifyOtp), "otp/verify")]
    [InlineData(nameof(AuthController.RefreshToken), "token/refresh")]
    [InlineData(nameof(AuthController.Logout), "session")]
    [InlineData(nameof(AuthController.LogoutAll), "sessions")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string expectedTemplate)
    {
        var method = typeof(AuthController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }

    private void SetRequestCookie(string value)
    {
        _controller.ControllerContext.HttpContext.Request.Headers.Cookie = $"{CookieName}={value}";
    }

    private void StubRefreshSuccess()
    {
        _mediator
            .Send(Arg.Any<RefreshTokenCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ServiceResult<AuthResult>.Success(NewAuthResult())));
    }

    private void StubVerifyOtpSuccess()
    {
        _mediator
            .Send(Arg.Any<VerifyOtpCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ServiceResult<AuthResult>.Success(NewAuthResult())));
    }

    private static AuthResult NewAuthResult() => new()
    {
        AccessToken = "new-access",
        RefreshToken = "new-refresh",
        AccessTokenExpiresAt = DateTime.UnixEpoch,
        RefreshTokenExpiresAt = DateTime.UnixEpoch,
        User = new UserProfileDto { PhoneNumber = "09123456789" },
        IsNewUser = false
    };
}
