using Application.Cart.Features.Commands.AddItemToCart;
using Application.Cart.Features.Commands.ClearCart;
using Application.Cart.Features.Commands.MergeGuestCart;
using Application.Cart.Features.Commands.RemoveItemFromCart;
using Application.Cart.Features.Commands.SyncCartPrices;
using Application.Cart.Features.Commands.UpdateCartItemQuantity;
using Application.Cart.Features.Queries.GetCart;
using Application.Cart.Features.Queries.GetCartSummary;
using Application.Cart.Features.Queries.ValidateCartForCheckout;
using Application.Cart.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Cart.Endpoints;
using Presentation.Cart.Requests;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using SharedKernel.Results;

namespace Tests.Presentation.Cart.Endpoints;

public class CartControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly CartController _controller;

    public CartControllerTests()
    {
        _controller = new CartController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetCart_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var expected = new CartDetailDto { Id = Guid.NewGuid(), TotalItems = 2, TotalPrice = 100 };

        _mediator.Send(Arg.Any<GetCartQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CartDetailDto>.Success(expected));

        // Act
        var result = await _controller.GetCart(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<CartDetailDto>>();
        body.Success.ShouldBeTrue();
        body.Data!.TotalItems.ShouldBe(2);
        await _mediator.Received(1).Send(Arg.Any<GetCartQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCartSummary_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var expected = new CartSummaryDto { ItemCount = 1, TotalQuantity = 3, TotalPrice = 150 };

        _mediator.Send(Arg.Any<GetCartSummaryQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CartSummaryDto>.Success(expected));

        // Act
        var result = await _controller.GetCartSummary(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<CartSummaryDto>>();
        body.Success.ShouldBeTrue();
        body.Data!.TotalQuantity.ShouldBe(3);
    }

    [Fact]
    public async Task ValidateCartForCheckout_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var expected = new CartCheckoutValidationDto { IsValid = true };

        _mediator.Send(Arg.Any<ValidateCartForCheckoutQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CartCheckoutValidationDto>.Success(expected));

        // Act
        var result = await _controller.ValidateCartForCheckout(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<CartCheckoutValidationDto>>();
        body.Success.ShouldBeTrue();
        body.Data!.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task AddItem_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var request = new AddCartItemRequest(variantId, 2);
        var expected = new CartDetailDto { Id = Guid.NewGuid(), TotalItems = 1 };

        _mediator.Send(Arg.Any<AddItemToCartCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CartDetailDto>.Success(expected));

        // Act
        var result = await _controller.AddItem(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<AddItemToCartCommand>(c => c.VariantId == variantId && c.Quantity == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateQuantity_WithValidRequest_SendsCommandWithRouteId_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var request = new UpdateCartItemQuantityRequest(5);
        var expected = new CartDetailDto { Id = Guid.NewGuid() };

        _mediator.Send(Arg.Any<UpdateCartItemQuantityCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CartDetailDto>.Success(expected));

        // Act
        var result = await _controller.UpdateQuantity(variantId, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateCartItemQuantityCommand>(c => c.VariantId == variantId && c.Quantity == 5),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveItem_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var expected = new CartDetailDto { Id = Guid.NewGuid() };

        _mediator.Send(Arg.Any<RemoveItemFromCartCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CartDetailDto>.Success(expected));

        // Act
        var result = await _controller.RemoveItem(variantId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<RemoveItemFromCartCommand>(c => c.VariantId == variantId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearCart_SendsCommand_AndReturnsNoContentStatus()
    {
        // Arrange
        _mediator.Send(Arg.Any<ClearCartCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ClearCart(CancellationToken.None);

        // Assert
        // Note: ToActionResult with a status override keeps the OkObjectResult
        // runtime type (derived from ObjectResult) and only changes StatusCode.
        var obj = result.ShouldBeAssignableTo<ObjectResult>();
        obj.StatusCode.ShouldBe(StatusCodes.Status204NoContent);
        await _mediator.Received(1).Send(Arg.Any<ClearCartCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MergeCart_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var expected = new CartDetailDto { Id = Guid.NewGuid() };

        _mediator.Send(Arg.Any<MergeGuestCartCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CartDetailDto>.Success(expected));

        // Act
        var result = await _controller.MergeCart(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(Arg.Any<MergeGuestCartCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncCartPrices_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var expected = new CartDetailDto { Id = Guid.NewGuid() };

        _mediator.Send(Arg.Any<SyncCartPricesCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CartDetailDto>.Success(expected));

        // Act
        var result = await _controller.SyncCartPrices(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(Arg.Any<SyncCartPricesCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CartController_HasRouteAttribute()
    {
        var routeAttr = typeof(CartController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/cart");
    }

    [Fact]
    public void CartController_HasAuthorizeAttribute()
    {
        typeof(CartController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(nameof(CartController.GetCart), null)]
    [InlineData(nameof(CartController.GetCartSummary), "summary")]
    [InlineData(nameof(CartController.ValidateCartForCheckout), "checkout/validation")]
    [InlineData(nameof(CartController.AddItem), "items")]
    [InlineData(nameof(CartController.UpdateQuantity), "items/{variantId:guid}")]
    [InlineData(nameof(CartController.RemoveItem), "items/{variantId:guid}")]
    [InlineData(nameof(CartController.MergeCart), "merge")]
    [InlineData(nameof(CartController.SyncCartPrices), "prices")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(CartController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
