using Application.Shipping.Features.Queries.GetShippings;
using Application.Shipping.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Shipping.Endpoints;
using SharedKernel.Results;

namespace Tests.Presentation.Shipping.Endpoints;

public class ShippingControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ShippingController _controller;

    public ShippingControllerTests()
    {
        _controller = new ShippingController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetActiveShippings_SendsQueryExcludingDeleted_AndReturnsOk()
    {
        // Arrange
        IReadOnlyList<ShippingListItemDto> expected =
            [new ShippingListItemDto { Id = Guid.NewGuid(), Name = "Post" }];

        _mediator.Send(Arg.Any<GetShippingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<ShippingListItemDto>>.Success(expected));

        // Act
        var result = await _controller.GetActiveShippings(CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<IReadOnlyList<ShippingListItemDto>>>();
        body.Data!.Count.ShouldBe(1);
        await _mediator.Received(1).Send(
            Arg.Is<GetShippingsQuery>(q => q.IncludeInactive == false),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void ShippingController_AllowsAnonymous()
    {
        typeof(ShippingController).GetCustomAttributes(typeof(AllowAnonymousAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ShippingController_HasRouteAttribute()
    {
        var routeAttr = typeof(ShippingController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/shipping");
    }
}
