using Application.Shipping.Features.Commands.CreateShipping;
using Application.Shipping.Features.Commands.DeleteShipping;
using Application.Shipping.Features.Commands.RestoreShipping;
using Application.Shipping.Features.Commands.UpdateShipping;
using Application.Shipping.Features.Queries.GetShipping;
using Application.Shipping.Features.Queries.GetShippings;
using Application.Shipping.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Shipping.Endpoints;
using Presentation.Shipping.Requests;
using SharedKernel.Results;

namespace Tests.Presentation.Shipping.Endpoints;

public class AdminShippingsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly AdminShippingsController _controller;

    public AdminShippingsControllerTests()
    {
        _controller = new AdminShippingsController(_mediator, _mapper);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetShippings_WithIncludeDeleted_SendsQuery_AndReturnsOk()
    {
        // Arrange
        IReadOnlyList<ShippingListItemDto> expected =
            [new ShippingListItemDto { Id = Guid.NewGuid(), Name = "Post" }];

        _mediator.Send(Arg.Any<GetShippingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<ShippingListItemDto>>.Success(expected));

        // Act
        var result = await _controller.GetShippings(true);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<IReadOnlyList<ShippingListItemDto>>>();
        body.Data!.Count.ShouldBe(1);
        await _mediator.Received(1).Send(
            Arg.Is<GetShippingsQuery>(q => q.IncludeInactive == true),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShippingById_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new ShippingDto { Id = id, Name = "Post" };

        _mediator.Send(Arg.Is<GetShippingQuery>(q => q.Id == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ShippingDto>.Success(expected));

        // Act
        var result = await _controller.GetShippingById(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<ShippingDto>>();
        body.Data!.Id.ShouldBe(id);
    }

    [Fact]
    public async Task CreateShipping_MapsRequestToCommand_AndReturnsOk()
    {
        // Arrange
        var request = new CreateShippingRequest("Post", 50000, null, null, 1, 3);
        var command = new CreateShippingCommand("Post", 50000, null, null, 1, 3);
        var expected = new ShippingDto { Id = Guid.NewGuid(), Name = "Post" };

        _mapper.Map<CreateShippingCommand>(request).Returns(command);
        _mediator.Send(Arg.Any<CreateShippingCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ShippingDto>.Success(expected));

        // Act
        var result = await _controller.CreateShipping(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<ShippingDto>>();
        body.Data!.Name.ShouldBe("Post");
    }

    [Fact]
    public async Task RestoreShipping_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<RestoreShippingCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.RestoreShipping(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<RestoreShippingCommand>(c => c.Id == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateShipping_MapsRequestWithRouteId_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateShippingRequest("Post", 60000, null, null, 2, 4);
        var mapped = new UpdateShippingCommand(Guid.Empty, "Post", 60000, null, null, 2, 4);
        var expected = new ShippingDto { Id = id, Name = "Post", BaseCost = 60000 };

        _mapper.Map<UpdateShippingCommand>(request).Returns(mapped);
        _mediator.Send(Arg.Any<UpdateShippingCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ShippingDto>.Success(expected));

        // Act
        var result = await _controller.UpdateShipping(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateShippingCommand>(c => c.Id == id && c.BaseCost == 60000),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteShipping_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteShippingCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DeleteShipping(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeleteShippingCommand>(c => c.Id == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShippingById_WhenNotFound_MapsToNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<GetShippingQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ShippingDto>.NotFound());

        // Act
        var result = await _controller.GetShippingById(id, CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void AdminShippingsController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminShippingsController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminShippingsController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminShippingsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/shipping");
    }

    [Theory]
    [InlineData(nameof(AdminShippingsController.GetShippings), null)]
    [InlineData(nameof(AdminShippingsController.GetShippingById), "{id:guid}")]
    [InlineData(nameof(AdminShippingsController.CreateShipping), null)]
    [InlineData(nameof(AdminShippingsController.RestoreShipping), "{id:guid}/restore")]
    [InlineData(nameof(AdminShippingsController.UpdateShipping), "{id:guid}")]
    [InlineData(nameof(AdminShippingsController.DeleteShipping), "{id:guid}")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AdminShippingsController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
