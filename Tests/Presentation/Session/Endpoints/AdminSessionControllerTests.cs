using Application.Auth.Features.Commands.AdminRevokeSession;
using Application.Auth.Features.Commands.LogoutAll;
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

public class AdminSessionControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AdminSessionController _controller;

    public AdminSessionControllerTests()
    {
        _controller = new AdminSessionController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetUserSessions_WithUserId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
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
        var result = await _controller.GetUserSessions(userId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<UserSessionDto>>>();
        body.Data!.Items.ShouldHaveSingleItem();
        await _mediator.Received(1).Send(
            Arg.Is<GetUserSessionsQuery>(q => q.TargetUserId == userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeUserSession_WithIds_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        _mediator.Send(Arg.Any<AdminRevokeSessionCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.RevokeUserSession(userId, sessionId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<AdminRevokeSessionCommand>(c => c.TargetUserId == userId && c.SessionId == sessionId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RevokeAllUserSessions_WithUserId_SendsLogoutAll_AndReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();

        _mediator.Send(Arg.Any<LogoutAllCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.RevokeAllUserSessions(userId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<LogoutAllCommand>(c => c.TargetUserId == userId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AdminSessionController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminSessionController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminSessionController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminSessionController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/users/{userId:guid}/sessions");
    }

    [Theory]
    [InlineData(nameof(AdminSessionController.GetUserSessions), null)]
    [InlineData(nameof(AdminSessionController.RevokeUserSession), "{sessionId:guid}")]
    [InlineData(nameof(AdminSessionController.RevokeAllUserSessions), null)]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AdminSessionController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
