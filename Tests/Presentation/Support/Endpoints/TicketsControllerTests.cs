using Application.Support.Features.Commands.CloseTicket;
using Application.Support.Features.Commands.CreateTicket;
using Application.Support.Features.Commands.ReplyToTicket;
using Application.Support.Features.Queries.GetTicketDetails;
using Application.Support.Features.Queries.GetTickets;
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

public class TicketsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly TicketsController _controller;

    public TicketsControllerTests()
    {
        _controller = new TicketsController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetMyTickets_WithFilters_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var paged = new PaginatedResult<TicketListItemDto>
        {
            Items = [new TicketListItemDto { Id = Guid.NewGuid(), Subject = "Issue" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetTicketsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<TicketListItemDto>>.Success(paged));

        // Act
        var result = await _controller.GetMyTickets("Open", null, 1, 10);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<TicketListItemDto>>>();
        body.Data!.Items.ShouldHaveSingleItem();
        await _mediator.Received(1).Send(
            Arg.Is<GetTicketsQuery>(q => q.Status == "Open" && q.Page == 1 && q.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetTicketDetails_WithValidId_SendsNonAdminQuery_AndReturnsOk()
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
            Arg.Is<GetTicketDetailsQuery>(q => q.TicketId == id && !q.IsAdmin),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateTicket_WithValidRequest_MapsToCommand_AndReturnsOk()
    {
        // Arrange
        var request = new CreateTicketRequest("Subject", "Order", "High", "Message");
        var expected = new TicketDto { Id = Guid.NewGuid(), Subject = "Subject" };

        _mediator.Send(Arg.Any<CreateTicketCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<TicketDto>.Success(expected));

        // Act
        var result = await _controller.CreateTicket(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<CreateTicketCommand>(c =>
                c.Subject == "Subject" &&
                c.Category == "Order" &&
                c.Priority == "High" &&
                c.Message == "Message"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReplyToTicket_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new ReplyToTicketRequest("Extra info");

        _mediator.Send(Arg.Any<ReplyToTicketCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ReplyToTicket(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ReplyToTicketCommand>(c => c.TicketId == id && c.Content == "Extra info"),
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
    public void TicketsController_HasAuthorizeAttribute()
    {
        typeof(TicketsController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void TicketsController_HasRouteAttribute()
    {
        var routeAttr = typeof(TicketsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/tickets");
    }

    [Theory]
    [InlineData(nameof(TicketsController.GetMyTickets), null)]
    [InlineData(nameof(TicketsController.GetTicketDetails), "{id:guid}")]
    [InlineData(nameof(TicketsController.CreateTicket), null)]
    [InlineData(nameof(TicketsController.ReplyToTicket), "{id:guid}/replies")]
    [InlineData(nameof(TicketsController.CloseTicket), "{id:guid}/status")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(TicketsController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
