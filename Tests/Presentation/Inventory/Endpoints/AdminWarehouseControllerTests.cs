using Application.Inventory.Features.Commands.CreateWarehouse;
using Application.Inventory.Features.Commands.DeleteWarehouse;
using Application.Inventory.Features.Commands.SetDefaultWarehouse;
using Application.Inventory.Features.Commands.ToggleWarehouseActive;
using Application.Inventory.Features.Commands.UpdateWarehouse;
using Application.Inventory.Features.Queries.GetAllWarehouses;
using Application.Inventory.Features.Queries.GetWarehouseById;
using Application.Inventory.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Inventory.Endpoints;
using Presentation.Inventory.Requests;
using SharedKernel.Results;

namespace Tests.Presentation.Inventory.Endpoints;

public class AdminWarehouseControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AdminWarehouseController _controller;

    public AdminWarehouseControllerTests()
    {
        _controller = new AdminWarehouseController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetAll_SendsQuery_AndReturnsOk()
    {
        // Arrange
        IReadOnlyList<WarehouseDto> expected =
        [
            new(Guid.NewGuid(), "WH-1", "Main", "Tehran", null, null, 1, true, true, DateTime.UtcNow)
        ];

        _mediator.Send(Arg.Any<GetAllWarehousesQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<WarehouseDto>>.Success(expected));

        // Act
        var result = await _controller.GetAll(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<IReadOnlyList<WarehouseDto>>>();
        body.Success.ShouldBeTrue();
        body.Data!.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetById_WithValidId_SendsQueryWithId_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new WarehouseDto(id, "WH-1", "Main", "Tehran", null, null, 1, true, true, DateTime.UtcNow);

        _mediator.Send(Arg.Is<GetWarehouseByIdQuery>(q => q.Id == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<WarehouseDto>.Success(expected));

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<WarehouseDto>>();
        body.Data!.Id.ShouldBe(id);
    }

    [Fact]
    public async Task Create_WithValidRequest_MapsToCommand_AndReturnsOk()
    {
        // Arrange
        var request = new CreateWarehouseRequest("WH-1", "Main", "Tehran", "Addr", "021", 1, true);

        _mediator.Send(Arg.Any<CreateWarehouseCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.Create(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<CreateWarehouseCommand>(c => c.Code == "WH-1" && c.Name == "Main" && c.City == "Tehran" && c.IsDefault),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WithValidRequest_MapsToCommandWithRouteId_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateWarehouseRequest("Main", "Tehran", "Addr", "021", 2);

        _mediator.Send(Arg.Any<UpdateWarehouseCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.Update(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateWarehouseCommand>(c => c.Id == id && c.Name == "Main" && c.Priority == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteWarehouseCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.Delete(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeleteWarehouseCommand>(c => c.Id == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetDefaultWarehouse_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<SetDefaultWarehouseCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.SetDefaultWarehouse(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<SetDefaultWarehouseCommand>(c => c.Id == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ToggleStatus_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new ToggleWarehouseStatusRequest(false);

        _mediator.Send(Arg.Any<ToggleWarehouseActiveCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ToggleStatus(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ToggleWarehouseActiveCommand>(c => c.Id == id && !c.IsActive),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_WhenNotFound_MapsToNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<GetWarehouseByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<WarehouseDto>.NotFound());

        // Act
        var result = await _controller.GetById(id, CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void AdminWarehouseController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminWarehouseController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminWarehouseController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminWarehouseController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/warehouses");
    }

    [Theory]
    [InlineData(nameof(AdminWarehouseController.GetAll), null)]
    [InlineData(nameof(AdminWarehouseController.GetById), "{id:guid}")]
    [InlineData(nameof(AdminWarehouseController.Create), null)]
    [InlineData(nameof(AdminWarehouseController.Update), "{id:guid}")]
    [InlineData(nameof(AdminWarehouseController.Delete), "{id:guid}")]
    [InlineData(nameof(AdminWarehouseController.SetDefaultWarehouse), "{id:guid}/set-default-warehouse")]
    [InlineData(nameof(AdminWarehouseController.ToggleStatus), "{id:guid}/status")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AdminWarehouseController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
