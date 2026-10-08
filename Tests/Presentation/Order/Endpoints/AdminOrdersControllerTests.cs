using Application.Order.Features.Commands.DeleteOrder;
using Application.Order.Features.Commands.ExpireOrders;
using Application.Order.Features.Commands.MarkOrderAsShipped;
using Application.Order.Features.Commands.UpdateOrderStatus;
using Application.Order.Features.Queries.GetAdminOrderById;
using Application.Order.Features.Queries.GetAdminOrders;
using Application.Order.Features.Queries.GetOrderStatistics;
using Application.Order.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Order.Endpoints;
using Presentation.Order.Requests;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Order.Endpoints;

public class AdminOrdersControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly AdminOrdersController _controller;

    public AdminOrdersControllerTests()
    {
        _controller = new AdminOrdersController(_mediator, _mapper);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetOrders_MapsRequestToQuery_AndReturnsOk()
    {
        // Arrange
        var request = new GetAdminOrdersRequest(Status: "Pending", Page: 1, PageSize: 10);
        var query = new GetAdminOrdersQuery("Pending", null, null, null, 1, 10);
        var paged = new PaginatedResult<AdminOrderDto>
        {
            Items = [new AdminOrderDto { Id = Guid.NewGuid(), OrderNumber = "ORD-1" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mapper.Map<GetAdminOrdersQuery>(request).Returns(query);
        _mediator.Send(Arg.Any<GetAdminOrdersQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<AdminOrderDto>>.Success(paged));

        // Act
        var result = await _controller.GetOrders(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<AdminOrderDto>>>();
        body.Success.ShouldBeTrue();
        body.Data!.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task GetOrderById_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new AdminOrderDto { Id = id, OrderNumber = "ORD-1" };

        _mediator.Send(Arg.Is<GetAdminOrderByIdQuery>(q => q.OrderId == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminOrderDto>.Success(expected));

        // Act
        var result = await _controller.GetOrderById(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<AdminOrderDto>>();
        body.Data!.Id.ShouldBe(id);
    }

    [Fact]
    public async Task GetStatistics_MapsRequestToQuery_AndReturnsOk()
    {
        // Arrange
        var request = new GetOrderStatisticsRequest();
        var query = new GetOrderStatisticsQuery();
        var expected = new OrderStatisticsDto { TotalOrders = 42, TotalRevenue = 100000 };

        _mapper.Map<GetOrderStatisticsQuery>(request).Returns(query);
        _mediator.Send(Arg.Any<GetOrderStatisticsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<OrderStatisticsDto>.Success(expected));

        // Act
        var result = await _controller.GetStatistics(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<OrderStatisticsDto>>();
        body.Data!.TotalOrders.ShouldBe(42);
    }

    [Fact]
    public async Task ExpireOrders_SendsCommand_AndReturnsOk()
    {
        // Arrange
        _mediator.Send(Arg.Any<ExpireOrdersCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<int>.Success(3));

        // Act
        var result = await _controller.ExpireOrders(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<int>>();
        body.Data.ShouldBe(3);
        await _mediator.Received(1).Send(Arg.Any<ExpireOrdersCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateOrderStatus_WithQuotedIfMatch_StripsQuotes_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateOrderStatusByIdRequest("Shipped");

        _mediator.Send(Arg.Any<UpdateOrderStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.UpdateOrderStatus(id, request, "\"rv-1\"", CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateOrderStatusCommand>(c => c.OrderId == id && c.NewStatus == "Shipped" && c.RowVersion == "rv-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteOrder_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DeleteOrder(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeleteOrderCommand>(c => c.OrderId == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkAsShipped_WithNullIfMatch_SendsNullRowVersion_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<MarkOrderAsShippedCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.MarkAsShipped(id, null, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<MarkOrderAsShippedCommand>(c => c.OrderId == id && c.RowVersion == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrderById_WhenNotFound_MapsToNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<GetAdminOrderByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminOrderDto>.NotFound());

        // Act
        var result = await _controller.GetOrderById(id, CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void AdminOrdersController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminOrdersController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminOrdersController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminOrdersController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/orders");
    }

    [Theory]
    [InlineData(nameof(AdminOrdersController.GetOrders), null)]
    [InlineData(nameof(AdminOrdersController.GetOrderById), "{id:guid}")]
    [InlineData(nameof(AdminOrdersController.GetStatistics), "statistics")]
    [InlineData(nameof(AdminOrdersController.ExpireOrders), "expiration")]
    [InlineData(nameof(AdminOrdersController.UpdateOrderStatus), "{id:guid}/status")]
    [InlineData(nameof(AdminOrdersController.DeleteOrder), "{id:guid}")]
    [InlineData(nameof(AdminOrdersController.MarkAsShipped), "{id:guid}/ship")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AdminOrdersController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
