using Application.Wishlist.Features.Queries.GetWishlistById;
using Application.Wishlist.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Wishlist.Endpoints;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Wishlist.Endpoints;

public class AdminWishlistControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AdminWishlistController _controller;

    public AdminWishlistControllerTests()
    {
        _controller = new AdminWishlistController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetUserWishlist_WithUserId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
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
        var result = await _controller.GetUserWishlist(userId, 1, 10);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<WishlistItemDto>>>();
        body.Data!.Items.ShouldHaveSingleItem();
        await _mediator.Received(1).Send(
            Arg.Is<GetWishlistByIdQuery>(q => q.TargetUserId == userId && q.Page == 1 && q.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AdminWishlistController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminWishlistController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminWishlistController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminWishlistController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/wishlist");
    }

    [Fact]
    public void GetUserWishlist_HasHttpGetWithUserIdTemplate()
    {
        var method = typeof(AdminWishlistController).GetMethod(nameof(AdminWishlistController.GetUserWishlist));
        method.ShouldNotBeNull();
        var httpGet = method!.GetCustomAttributes(typeof(HttpGetAttribute), false)
            .OfType<HttpGetAttribute>()
            .SingleOrDefault();
        httpGet.ShouldNotBeNull();
        httpGet!.Template.ShouldBe("{userId:guid}/wishlist");
    }
}
