using Application.Order.Features.Queries.GetOrderStatus;
using Application.Order.Features.Queries.GetOrderStatuses;
using Application.Order.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Order.Endpoints;
using SharedKernel.Results;

namespace Tests.Presentation.Order.Endpoints;

public class OrderStatusControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly OrderStatusController _controller;

    public OrderStatusControllerTests()
    {
        _controller = new OrderStatusController(_mediator, Substitute.For<IMapper>());

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetOrderStatuses_SendsOnlyActiveQuery_AndReturnsOk()
    {
        // Arrange
        IReadOnlyList<OrderStatusDto> expected =
            [new OrderStatusDto { Id = Guid.NewGuid(), Name = "Pending" }];

        _mediator.Send(Arg.Any<GetOrderStatusesQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<OrderStatusDto>>.Success(expected));

        // Act
        var result = await _controller.GetOrderStatuses(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<IReadOnlyList<OrderStatusDto>>>();
        body.Success.ShouldBeTrue();
        body.Data!.Count.ShouldBe(1);
        await _mediator.Received(1).Send(
            Arg.Is<GetOrderStatusesQuery>(q => q.OnlyActive == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrderStatusById_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new OrderStatusDto { Id = id, Name = "Pending" };

        _mediator.Send(Arg.Is<GetOrderStatusQuery>(q => q.Id == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<OrderStatusDto>.Success(expected));

        // Act
        var result = await _controller.GetOrderStatusById(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<OrderStatusDto>>();
        body.Data!.Id.ShouldBe(id);
    }

    [Fact]
    public void OrderStatusController_AllowsAnonymous()
    {
        typeof(OrderStatusController).GetCustomAttributes(typeof(AllowAnonymousAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void OrderStatusController_HasRouteAttribute()
    {
        var routeAttr = typeof(OrderStatusController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/order-statuses");
    }

    [Theory]
    [InlineData(nameof(OrderStatusController.GetOrderStatuses), null)]
    [InlineData(nameof(OrderStatusController.GetOrderStatusById), "{id:guid}")]
    public void Actions_HaveExpectedHttpGetTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(OrderStatusController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var httpGet = method!.GetCustomAttributes(typeof(HttpGetAttribute), false)
            .OfType<HttpGetAttribute>()
            .SingleOrDefault();
        httpGet.ShouldNotBeNull();
        httpGet!.Template.ShouldBe(expectedTemplate);
    }
}
