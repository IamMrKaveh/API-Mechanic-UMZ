using Application.Order.Features.Commands.CancelOrder;
using Application.Order.Features.Commands.CheckoutFromCart;
using Application.Order.Features.Commands.ConfirmDelivery;
using Application.Order.Features.Commands.RequestReturn;
using Application.Order.Features.Queries.GetOrderDetails;
using Application.Order.Features.Queries.GetUserOrders;
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

public class OrdersControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        _controller = new OrdersController(_mediator, Substitute.For<IMapper>());

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
        var request = new GetUserOrdersRequest("Pending", 1, 10);
        var paged = new PaginatedResult<OrderListItemDto>
        {
            Items = [new OrderListItemDto { Id = Guid.NewGuid(), OrderNumber = "ORD-1" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetUserOrdersQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<OrderListItemDto>>.Success(paged));

        // Act
        var result = await _controller.GetOrders(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<OrderListItemDto>>>();
        body.Success.ShouldBeTrue();
        await _mediator.Received(1).Send(
            Arg.Is<GetUserOrdersQuery>(q => q.Status == "Pending" && q.Page == 1 && q.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrderById_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new OrderDto { Id = id, OrderNumber = "ORD-1" };

        _mediator.Send(Arg.Is<GetOrderDetailsQuery>(q => q.OrderId == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<OrderDto>.Success(expected));

        // Act
        var result = await _controller.GetOrderById(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<OrderDto>>();
        body.Data!.Id.ShouldBe(id);
    }

    [Fact]
    public async Task CheckoutFromCart_WithValidRequest_SendsCommand_AndReturnsCreated()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var shippingId = Guid.NewGuid();
        var addressId = Guid.NewGuid();
        var request = new CheckoutFromCartRequest(cartId, shippingId, addressId, "SAVE10", "ZarinPal", null);
        var expected = new CheckoutResultDto { OrderId = Guid.NewGuid(), OrderNumber = "ORD-1" };

        _mediator.Send(Arg.Any<CheckoutFromCartCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CheckoutResultDto>.Success(expected));

        // Act
        var result = await _controller.CheckoutFromCart(request, CancellationToken.None);

        // Assert
        var created = result.ShouldBeOfType<ObjectResult>();
        created.StatusCode.ShouldBe(StatusCodes.Status201Created);
        var body = created.Value.ShouldBeOfType<ApiResponse<CheckoutResultDto>>();
        body.Data!.OrderNumber.ShouldBe("ORD-1");
        await _mediator.Received(1).Send(
            Arg.Is<CheckoutFromCartCommand>(c =>
                c.CartId == cartId &&
                c.ShippingId == shippingId &&
                c.AddressId == addressId &&
                c.DiscountCode == "SAVE10"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CancelOrder_WithQuotedIfMatch_StripsQuotes_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new CancelOrderRequest("changed mind");

        _mediator.Send(Arg.Any<CancelOrderCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.CancelOrder(id, request, "\"rv-1\"", CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<CancelOrderCommand>(c => c.OrderId == id && c.Reason == "changed mind" && c.RowVersion == "rv-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmDelivery_WithNullIfMatch_SendsNullRowVersion_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<ConfirmDeliveryCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ConfirmDelivery(id, null, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ConfirmDeliveryCommand>(c => c.OrderId == id && c.RowVersion == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestReturn_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new RequestReturnRequest("defective");

        _mediator.Send(Arg.Any<RequestReturnCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.RequestReturn(id, request, null, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<RequestReturnCommand>(c => c.OrderId == id && c.Reason == "defective"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrderById_WhenNotFound_MapsToNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<GetOrderDetailsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<OrderDto>.NotFound());

        // Act
        var result = await _controller.GetOrderById(id, CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void OrdersController_HasAuthorizeAttribute()
    {
        typeof(OrdersController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void OrdersController_HasRouteAttribute()
    {
        var routeAttr = typeof(OrdersController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/orders");
    }

    [Theory]
    [InlineData(nameof(OrdersController.GetOrders), null)]
    [InlineData(nameof(OrdersController.GetOrderById), "{id:guid}")]
    [InlineData(nameof(OrdersController.CheckoutFromCart), null)]
    [InlineData(nameof(OrdersController.CancelOrder), "{id:guid}/cancellation")]
    [InlineData(nameof(OrdersController.ConfirmDelivery), "{id:guid}/delivery-confirmation")]
    [InlineData(nameof(OrdersController.RequestReturn), "{id:guid}/return-request")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(OrdersController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
