using Application.Notification.Features.Commands.DeleteNotification;
using Application.Notification.Features.Commands.MarkAllNotificationsRead;
using Application.Notification.Features.Commands.MarkNotificationRead;
using Application.Notification.Features.Queries.GetNotifications;
using Application.Notification.Features.Queries.GetUnreadNotificationCount;
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

public class NotificationsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly NotificationsController _controller;

    public NotificationsControllerTests()
    {
        _controller = new NotificationsController(_mediator, Substitute.For<IMapper>());

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetMyNotifications_MapsRequestToQuery_AndReturnsOk()
    {
        // Arrange
        var request = new GetNotificationsRequest(UnreadOnly: true, Page: 2, PageSize: 10);
        var paged = new PaginatedResult<NotificationDto>
        {
            Items = [new NotificationDto { Id = Guid.NewGuid(), Title = "T", Message = "M", Type = "Info" }],
            TotalCount = 1,
            Page = 2,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetNotificationsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<NotificationDto>>.Success(paged));

        // Act
        var result = await _controller.GetMyNotifications(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<NotificationDto>>>();
        body.Success.ShouldBeTrue();
        await _mediator.Received(1).Send(
            Arg.Is<GetNotificationsQuery>(q => q.UnreadOnly && q.Page == 2 && q.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetUnreadCount_SendsQuery_AndReturnsOk()
    {
        // Arrange
        _mediator.Send(Arg.Any<GetUnreadNotificationCountQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<int>.Success(3));

        // Act
        var result = await _controller.GetUnreadCount(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<int>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldBe(3);
    }

    [Fact]
    public async Task MarkAsRead_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<MarkNotificationReadCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.MarkAsRead(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<MarkNotificationReadCommand>(c => c.NotificationId == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAllAsRead_SendsCommand_AndReturnsOk()
    {
        // Arrange
        _mediator.Send(Arg.Any<MarkAllNotificationsReadCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.MarkAllAsRead(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(Arg.Any<MarkAllNotificationsReadCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteNotification_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DeleteNotification(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeleteNotificationCommand>(c => c.NotificationId == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAsRead_WhenNotFound_MapsToNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<MarkNotificationReadCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.NotFound());

        // Act
        var result = await _controller.MarkAsRead(id, CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void NotificationsController_HasAuthorizeAttribute()
    {
        typeof(NotificationsController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void NotificationsController_HasRouteAttribute()
    {
        var routeAttr = typeof(NotificationsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/notifications");
    }

    [Fact]
    public void GetUnreadCount_AllowsAnonymous()
    {
        var method = typeof(NotificationsController).GetMethod(nameof(NotificationsController.GetUnreadCount));
        method.ShouldNotBeNull();
        method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), false).Length.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(nameof(NotificationsController.GetMyNotifications), null)]
    [InlineData(nameof(NotificationsController.GetUnreadCount), "unread-count")]
    [InlineData(nameof(NotificationsController.MarkAsRead), "{id:guid}/read")]
    [InlineData(nameof(NotificationsController.MarkAllAsRead), "read")]
    [InlineData(nameof(NotificationsController.DeleteNotification), "{id:guid}")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(NotificationsController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
