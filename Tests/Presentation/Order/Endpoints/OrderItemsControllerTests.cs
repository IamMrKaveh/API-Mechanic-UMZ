using Application.Order.Features.Commands.DeleteOrderItem;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Order.Endpoints;
using SharedKernel.Results;

namespace Tests.Presentation.Order.Endpoints;

public class OrderItemsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly OrderItemsController _controller;

    public OrderItemsControllerTests()
    {
        _controller = new OrderItemsController(_mediator, Substitute.For<IMapper>());

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task DeleteOrderItem_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteOrderItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DeleteOrderItem(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeleteOrderItemCommand>(c => c.Id == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteOrderItem_WhenNotFound_MapsToNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteOrderItemCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.NotFound());

        // Act
        var result = await _controller.DeleteOrderItem(id, CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void OrderItemsController_HasAuthorizeAttribute()
    {
        typeof(OrderItemsController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void OrderItemsController_HasRouteAttribute()
    {
        var routeAttr = typeof(OrderItemsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/order-items");
    }

    [Fact]
    public void DeleteOrderItem_HasHttpDeleteWithIdTemplate()
    {
        var method = typeof(OrderItemsController).GetMethod(nameof(OrderItemsController.DeleteOrderItem));
        method.ShouldNotBeNull();
        var httpDelete = method!.GetCustomAttributes(typeof(HttpDeleteAttribute), false)
            .OfType<HttpDeleteAttribute>()
            .SingleOrDefault();
        httpDelete.ShouldNotBeNull();
        httpDelete!.Template.ShouldBe("{id:guid}");
    }
}
