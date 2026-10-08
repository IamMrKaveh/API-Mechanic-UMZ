using Application.Support.Features.Commands.CloseTicket;
using Application.Support.Features.Commands.ReplyToTicket;
using Application.Support.Features.Queries.GetAdminTickets;
using Application.Support.Features.Queries.GetTicketDetails;
using Application.Support.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Support.Endpoints;
using Presentation.Support.Requests;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Support.Endpoints;

public class AdminTicketsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AdminTicketsController _controller;

    public AdminTicketsControllerTests()
    {
        _controller = new AdminTicketsController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetTickets_WithFilters_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var paged = new PaginatedResult<TicketDto>
        {
            Items = [new TicketDto { Id = Guid.NewGuid(), Subject = "Issue" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 20
        };

        _mediator.Send(Arg.Any<GetAdminTicketsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<TicketDto>>.Success(paged));

        // Act
        var result = await _controller.GetTickets("Open", "High", 1, 20);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<TicketDto>>>();
        body.Data!.Items.ShouldHaveSingleItem();
        await _mediator.Received(1).Send(
            Arg.Is<GetAdminTicketsQuery>(q => q.Status == "Open" && q.Priority == "High"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTicketDetails_WithValidId_SendsAdminQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new TicketDto { Id = id, Subject = "Issue" };

        _mediator.Send(Arg.Any<GetTicketDetailsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<TicketDto>.Success(expected));

        // Act
        var result = await _controller.GetTicketDetails(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetTicketDetailsQuery>(q => q.TicketId == id && q.IsAdmin),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplyToTicket_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new ReplyToTicketRequest("We are on it");

        _mediator.Send(Arg.Any<ReplyToTicketCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ReplyToTicket(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ReplyToTicketCommand>(c => c.TicketId == id && c.Content == "We are on it"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CloseTicket_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<CloseTicketCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.CloseTicket(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<CloseTicketCommand>(c => c.TicketId == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AdminTicketsController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminTicketsController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminTicketsController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminTicketsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/tickets");
    }

    [Theory]
    [InlineData(nameof(AdminTicketsController.GetTickets), null)]
    [InlineData(nameof(AdminTicketsController.GetTicketDetails), "{id:guid}")]
    [InlineData(nameof(AdminTicketsController.ReplyToTicket), "{id:guid}/replies")]
    [InlineData(nameof(AdminTicketsController.CloseTicket), "{id:guid}/status")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AdminTicketsController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
