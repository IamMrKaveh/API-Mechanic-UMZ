using Application.Wishlist.Features.Commands.ClearWishlist;
using Application.Wishlist.Features.Commands.RemoveFromWishlist;
using Application.Wishlist.Features.Commands.ToggleWishlist;
using Application.Wishlist.Features.Queries.CheckWishlistStatus;
using Application.Wishlist.Features.Queries.GetWishlistById;
using Application.Wishlist.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Wishlist.Endpoints;
using Presentation.Wishlist.Requests;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Wishlist.Endpoints;

public class WishlistControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly WishlistController _controller;

    public WishlistControllerTests()
    {
        _controller = new WishlistController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetMyWishlist_WithDefaults_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var paged = new PaginatedResult<WishlistItemDto>
        {
            Items = [new WishlistItemDto(Guid.NewGuid(), Guid.NewGuid(), "P1", 100, true, null, DateTime.UtcNow)],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetWishlistByIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<WishlistItemDto>>.Success(paged));

        // Act
        var result = await _controller.GetMyWishlist();

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<WishlistItemDto>>>();
        body.Data!.Items.ShouldHaveSingleItem();
        await _mediator.Received(1).Send(
            Arg.Is<GetWishlistByIdQuery>(q => q.TargetUserId == null && q.Page == 1 && q.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task IsInWishlist_WithProductId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mediator.Send(Arg.Is<CheckWishlistStatusQuery>(q => q.ProductId == productId), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<bool>.Success(true));

        // Act
        var result = await _controller.IsInWishlist(productId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<bool>>();
        body.Data.ShouldBeTrue();
    }

    [Fact]
    public async Task ToggleWishlist_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var request = new ToggleWishlistRequest(productId);

        _mediator.Send(Arg.Any<ToggleWishlistCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<bool>.Success(true));

        // Act
        var result = await _controller.ToggleWishlist(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<bool>>();
        body.Data.ShouldBeTrue();
        await _mediator.Received(1).Send(
            Arg.Is<ToggleWishlistCommand>(c => c.ProductId == productId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveFromWishlist_WithProductId_SendsCommand_AndReturnsNoContent()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _mediator.Send(Arg.Any<RemoveFromWishlistCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.RemoveFromWishlist(productId, CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
        await _mediator.Received(1).Send(
            Arg.Is<RemoveFromWishlistCommand>(c => c.ProductId == productId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ClearWishlist_SendsCommand_AndReturnsNoContent()
    {
        // Arrange
        _mediator.Send(Arg.Any<ClearWishlistCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ClearWishlist(CancellationToken.None);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
        await _mediator.Received(1).Send(Arg.Any<ClearWishlistCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void WishlistController_HasAuthorizeAttribute()
    {
        typeof(WishlistController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void WishlistController_HasRouteAttribute()
    {
        var routeAttr = typeof(WishlistController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/wishlist");
    }

    [Theory]
    [InlineData(nameof(WishlistController.GetMyWishlist), null)]
    [InlineData(nameof(WishlistController.IsInWishlist), "{productId:guid}")]
    [InlineData(nameof(WishlistController.ToggleWishlist), null)]
    [InlineData(nameof(WishlistController.RemoveFromWishlist), "{productId:guid}")]
    [InlineData(nameof(WishlistController.ClearWishlist), null)]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(WishlistController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
