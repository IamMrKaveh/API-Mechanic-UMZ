using Application.Inventory.Features.Commands.AddStock;
using Application.Inventory.Features.Commands.RemoveStock;
using Application.Variant.Features.Commands.AddVariant;
using Application.Variant.Features.Commands.RemoveVariant;
using Application.Variant.Features.Commands.UpdateVariant;
using Application.Variant.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Variant.Endpoints;
using Presentation.Variant.Requests;
using SharedKernel.Results;

namespace Tests.Presentation.Variant.Endpoints;

public class AdminVariantControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AdminVariantController _controller;

    public AdminVariantControllerTests()
    {
        _controller = new AdminVariantController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task Add_WithValidRequest_SendsCommand_AndReturnsCreated()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new AddVariantRequest("SKU1", 100, 120, 10, false, 1, [Guid.NewGuid()], null);
        var expected = new ProductVariantViewDto { Id = Guid.NewGuid(), ProductId = productId, Sku = "SKU1" };

        _mediator.Send(Arg.Any<AddVariantCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ProductVariantViewDto>.Success(expected));

        // Act
        var result = await _controller.Add(productId, request, CancellationToken.None);

        // Assert
        // Note: ToActionResult with a status override keeps the OkObjectResult
        // runtime type (derived from ObjectResult) and only changes StatusCode.
        var created = result.ShouldBeAssignableTo<ObjectResult>();
        created.StatusCode.ShouldBe(StatusCodes.Status201Created);
        var body = created.Value.ShouldBeOfType<ApiResponse<ProductVariantViewDto>>();
        body.Success.ShouldBeTrue();
        await _mediator.Received(1).Send(
            Arg.Is<AddVariantCommand>(c => c.ProductId == productId && c.Sku == "SKU1" && c.SellingPrice == 100),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Add_WithEmptyProductId_ReturnsBadRequest_WithoutMediatorCall()
    {
        // Arrange
        var request = new AddVariantRequest("SKU1", 100, 120);

        // Act
        var result = await _controller.Add(Guid.Empty, request, CancellationToken.None);

        // Assert
        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        await _mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<AddVariantCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Add_WithNullRequest_ReturnsBadRequest_WithoutMediatorCall()
    {
        // Act
        var result = await _controller.Add(Guid.NewGuid(), null!, CancellationToken.None);

        // Assert
        var badRequest = result.ShouldBeOfType<BadRequestObjectResult>();
        badRequest.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
        await _mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<AddVariantCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddStock_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var request = new AddStockRequest(5, "restock");

        _mediator.Send(Arg.Any<AddStockCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.AddStock(variantId, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<AddStockCommand>(c => c.VariantId == variantId && c.Quantity == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var request = new UpdateVariantRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "SKU1", 100, 120, 10, false, 1, null, null);

        _mediator.Send(Arg.Any<UpdateVariantCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.Update(productId, variantId, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateVariantCommand>(c => c.ProductId == productId && c.VariantId == variantId && c.Sku == "SKU1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WithValidIds_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        _mediator.Send(Arg.Any<RemoveVariantCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.Delete(productId, variantId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<RemoveVariantCommand>(c => c.ProductId == productId && c.VariantId == variantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveStock_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var request = new RemoveStockRequest(2, "damage");

        _mediator.Send(Arg.Any<RemoveStockCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.RemoveStock(variantId, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<RemoveStockCommand>(c => c.VariantId == variantId && c.Quantity == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AdminVariantController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminVariantController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminVariantController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminVariantController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/products/variants");
    }

    [Theory]
    [InlineData(nameof(AdminVariantController.Add), null)]
    [InlineData(nameof(AdminVariantController.AddStock), "{variantId:guid}/stock")]
    [InlineData(nameof(AdminVariantController.Update), "{variantId:guid}")]
    [InlineData(nameof(AdminVariantController.Delete), "{variantId:guid}")]
    [InlineData(nameof(AdminVariantController.RemoveStock), "{variantId:guid}/stock")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AdminVariantController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
