using Application.Notification.Features.Commands.AdminDeleteNotification;
using Application.Notification.Features.Commands.AdminSendNotification;
using Application.Notification.Features.Queries.GetAllNotifications;
using Application.Notification.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Notification.Endpoints;
using Presentation.Notification.Requests;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Notification.Endpoints;

public class AdminNotificationControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AdminNotificationController _controller;

    public AdminNotificationControllerTests()
    {
        _controller = new AdminNotificationController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetAll_WithDefaults_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var paged = new PaginatedResult<NotificationDto>
        {
            Items = [new NotificationDto { Id = Guid.NewGuid(), Title = "T", Message = "M", Type = "Info" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mediator.Send(Arg.Any<GetAllNotificationsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<NotificationDto>>.Success(paged));

        // Act
        var result = await _controller.GetAll();

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<NotificationDto>>>();
        body.Success.ShouldBeTrue();
        body.Data!.Items.ShouldHaveSingleItem();
        await _mediator.Received(1).Send(
            Arg.Is<GetAllNotificationsQuery>(q => q.Page == 1 && q.PageSize == 20),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_WithSendToAll_MapsToCommand_AndReturnsOk()
    {
        // Arrange
        var request = new AdminSendNotificationRequest("T", "M", "Info", null, true, null);

        _mediator.Send(Arg.Any<AdminSendNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.Send(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<AdminSendNotificationCommand>(c =>
                c.Title == "T" &&
                c.Message == "M" &&
                c.Type == "Info" &&
                c.SendToAll &&
                c.UserId == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_WithTargetUser_MapsToCommand_AndReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new AdminSendNotificationRequest("T", "M", "Warning", "/orders", false, userId);

        _mediator.Send(Arg.Any<AdminSendNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.Send(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<AdminSendNotificationCommand>(c => !c.SendToAll && c.UserId == userId && c.ActionUrl == "/orders"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<AdminDeleteNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<AdminDeleteNotificationCommand>(c => c.NotificationId == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WhenNotFound_MapsToNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<AdminDeleteNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.NotFound());

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void AdminNotificationController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminNotificationController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminNotificationController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminNotificationController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/notifications");
    }

    [Theory]
    [InlineData(nameof(AdminNotificationController.GetAll), null)]
    [InlineData(nameof(AdminNotificationController.Send), "send")]
    [InlineData(nameof(AdminNotificationController.Delete), "{id:guid}")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AdminNotificationController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
