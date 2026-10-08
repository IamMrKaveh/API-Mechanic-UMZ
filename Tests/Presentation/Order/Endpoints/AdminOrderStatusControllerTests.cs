using Application.Order.Features.Commands.ActivateOrderStatus;
using Application.Order.Features.Commands.CreateOrderStatus;
using Application.Order.Features.Commands.DeactivateOrderStatus;
using Application.Order.Features.Commands.DeleteOrderStatus;
using Application.Order.Features.Commands.SetDefaultOrderStatus;
using Application.Order.Features.Commands.UpdateOrderStatusDefinition;
using Application.Order.Features.Queries.GetOrderStatus;
using Application.Order.Features.Queries.GetOrderStatuses;
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
using SharedKernel.Results;

namespace Tests.Presentation.Order.Endpoints;

public class AdminOrderStatusControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AdminOrderStatusController _controller;

    public AdminOrderStatusControllerTests()
    {
        _controller = new AdminOrderStatusController(_mediator, Substitute.For<IMapper>());

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetOrderStatuses_WithOnlyActive_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var request = new GetOrderStatusesRequest(true);
        IReadOnlyList<OrderStatusDto> expected =
            [new OrderStatusDto { Id = Guid.NewGuid(), Name = "Pending" }];

        _mediator.Send(Arg.Any<GetOrderStatusesQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<OrderStatusDto>>.Success(expected));

        // Act
        var result = await _controller.GetOrderStatuses(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetOrderStatusesQuery>(q => q.OnlyActive == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrderStatus_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new OrderStatusDto { Id = id, Name = "Pending" };

        _mediator.Send(Arg.Is<GetOrderStatusQuery>(q => q.Id == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<OrderStatusDto>.Success(expected));

        // Act
        var result = await _controller.GetOrderStatus(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<OrderStatusDto>>();
        body.Data!.Id.ShouldBe(id);
    }

    [Fact]
    public async Task CreateOrderStatus_WithValidRequest_MapsToCommand_AndReturnsOk()
    {
        // Arrange
        var request = new CreateOrderStatusRequest("pending", "Pending", null, null, 1, true, true);
        var expected = new OrderStatusDto { Id = Guid.NewGuid(), Name = "pending" };

        _mediator.Send(Arg.Any<CreateOrderStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<OrderStatusDto>.Success(expected));

        // Act
        var result = await _controller.CreateOrderStatus(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<CreateOrderStatusCommand>(c => c.Name == "pending" && c.SortOrder == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateOrderStatus_WithQuotedIfMatch_StripsQuotes_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateOrderStatusRequest("Pending", null, null, 2, true, true);

        _mediator.Send(Arg.Any<UpdateOrderStatusDefinitionCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.UpdateOrderStatus(id, request, "\"rv-1\"", CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateOrderStatusDefinitionCommand>(c => c.Id == id && c.RowVersion == "rv-1" && c.SortOrder == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateOrderStatus_WithNullIfMatch_SendsNullRowVersion()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateOrderStatusRequest("Pending", null, null, 2, true, true);

        _mediator.Send(Arg.Any<UpdateOrderStatusDefinitionCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.UpdateOrderStatus(id, request, null, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateOrderStatusDefinitionCommand>(c => c.Id == id && c.RowVersion == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteOrderStatus_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteOrderStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DeleteOrderStatus(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeleteOrderStatusCommand>(c => c.Id == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivateOrderStatus_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<ActivateOrderStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ActivateOrderStatus(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ActivateOrderStatusCommand>(c => c.Id == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateOrderStatus_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeactivateOrderStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DeactivateOrderStatus(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeactivateOrderStatusCommand>(c => c.Id == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetDefaultOrderStatus_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<SetDefaultOrderStatusCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.SetDefaultOrderStatus(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<SetDefaultOrderStatusCommand>(c => c.Id == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetOrderStatus_WhenNotFound_MapsToNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<GetOrderStatusQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<OrderStatusDto>.NotFound());

        // Act
        var result = await _controller.GetOrderStatus(id, CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void AdminOrderStatusController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminOrderStatusController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminOrderStatusController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminOrderStatusController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/order-statuses");
    }

    [Theory]
    [InlineData(nameof(AdminOrderStatusController.GetOrderStatuses), null)]
    [InlineData(nameof(AdminOrderStatusController.GetOrderStatus), "{id:guid}")]
    [InlineData(nameof(AdminOrderStatusController.CreateOrderStatus), null)]
    [InlineData(nameof(AdminOrderStatusController.UpdateOrderStatus), "{id:guid}")]
    [InlineData(nameof(AdminOrderStatusController.DeleteOrderStatus), "{id:guid}")]
    [InlineData(nameof(AdminOrderStatusController.ActivateOrderStatus), "{id:guid}/activate")]
    [InlineData(nameof(AdminOrderStatusController.DeactivateOrderStatus), "{id:guid}/deactivate")]
    [InlineData(nameof(AdminOrderStatusController.SetDefaultOrderStatus), "{id:guid}/set-default")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AdminOrderStatusController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
