using Application.Auth.Features.Commands.LogoutAll;
using Application.Auth.Features.Commands.LogoutOthers;
using Application.Auth.Features.Commands.RevokeSession;
using Application.Auth.Features.Queries.GetCurrentSession;
using Application.Auth.Features.Queries.GetUserSessions;
using Application.Auth.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Session.Endpoints;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Session.Endpoints;

public class SessionControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly SessionController _controller;

    public SessionControllerTests()
    {
        _controller = new SessionController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetActiveSessions_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var paged = new PaginatedResult<UserSessionDto>
        {
            Items = [new UserSessionDto { Id = Guid.NewGuid(), DeviceInfo = "Chrome" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetUserSessionsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<UserSessionDto>>.Success(paged));

        // Act
        var result = await _controller.GetActiveSessions(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<UserSessionDto>>>();
        body.Data!.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task GetCurrentSession_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var expected = new CurrentSessionDto { SessionId = Guid.NewGuid(), IsAuthenticated = true };

        _mediator.Send(Arg.Any<GetCurrentSessionQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CurrentSessionDto>.Success(expected));

        // Act
        var result = await _controller.GetCurrentSession(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<CurrentSessionDto>>();
        body.Data!.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public async Task RevokeSession_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _mediator.Send(Arg.Any<RevokeSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.RevokeSession(sessionId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<RevokeSessionCommand>(c => c.SessionId == sessionId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogoutOtherSessions_SendsCommand_AndReturnsOk()
    {
        // Arrange
        _mediator.Send(Arg.Any<LogoutOthersCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.LogoutOtherSessions(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(Arg.Any<LogoutOthersCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LogoutAllSessions_SendsCommand_AndReturnsOk()
    {
        // Arrange
        _mediator.Send(Arg.Any<LogoutAllCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.LogoutAllSessions(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(Arg.Any<LogoutAllCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void SessionController_HasAuthorizeAttribute()
    {
        typeof(SessionController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void SessionController_HasRouteAttribute()
    {
        var routeAttr = typeof(SessionController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/sessions");
    }

    [Theory]
    [InlineData(nameof(SessionController.GetActiveSessions), null)]
    [InlineData(nameof(SessionController.GetCurrentSession), "current")]
    [InlineData(nameof(SessionController.RevokeSession), "{sessionId:guid}")]
    [InlineData(nameof(SessionController.LogoutOtherSessions), "others")]
    [InlineData(nameof(SessionController.LogoutAllSessions), null)]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(SessionController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
