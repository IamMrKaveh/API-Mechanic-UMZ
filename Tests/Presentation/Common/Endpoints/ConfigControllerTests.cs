using Application.Auth.Features.Shared;
using Infrastructure.Auth.Options;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Presentation.Base.Responses;
using Presentation.Common.Endpoints;

namespace Tests.Presentation.Common.Endpoints;

public class ConfigControllerTests
{
    private static ConfigController BuildController(
        int otpLength = 6,
        int otpExpirationMinutes = 2,
        int sessionExpirationDays = 30)
    {
        var mediator = Substitute.For<IMediator>();
        return new ConfigController(
            Options.Create(new AuthOptions { SessionExpirationDays = sessionExpirationDays }),
            Options.Create(new OtpOptions { Length = otpLength, ExpirationMinutes = otpExpirationMinutes }),
            mediator);
    }

    [Fact]
    public void GetAuthConfig_ReturnsOkWithMappedDto()
    {
        var controller = BuildController(
            otpLength: 6,
            otpExpirationMinutes: 2,
            sessionExpirationDays: 30);

        var result = controller.GetAuthConfig();

        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(200);
        var body = ok.Value.ShouldBeOfType<ApiResponse<AuthPublicConfigDto>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldNotBeNull();
        body.Data!.OtpLength.ShouldBe(6);
        body.Data.OtpResendSeconds.ShouldBe(120);
        body.Data.OtpExpirationMinutes.ShouldBe(2);
        body.Data.SessionExpirationDays.ShouldBe(30);
    }

    [Fact]
    public void GetAuthConfig_ResendSeconds_IsExpirationMinutesTimesSixty()
    {
        var controller = BuildController(otpLength: 5, otpExpirationMinutes: 10, sessionExpirationDays: 7);

        var ok = controller.GetAuthConfig().ShouldBeOfType<OkObjectResult>();
        var body = ok.Value.ShouldBeOfType<ApiResponse<AuthPublicConfigDto>>();

        body.Data!.OtpLength.ShouldBe(5);
        body.Data.OtpResendSeconds.ShouldBe(600);
        body.Data.OtpExpirationMinutes.ShouldBe(10);
        body.Data.SessionExpirationDays.ShouldBe(7);
    }

    [Fact]
    public void GetAuthConfig_HasAllowAnonymousAttribute()
    {
        typeof(ConfigController).GetCustomAttributes(typeof(AllowAnonymousAttribute), true)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GetAuthConfigAction_HasHttpGetWithAuthTemplate()
    {
        var method = typeof(ConfigController).GetMethod(nameof(ConfigController.GetAuthConfig));
        method.ShouldNotBeNull();
        var httpGet = method!.GetCustomAttributes(typeof(HttpGetAttribute), false)
            .OfType<HttpGetAttribute>()
            .SingleOrDefault();
        httpGet.ShouldNotBeNull();
        httpGet!.Template.ShouldBe("auth");
    }
}
