using Application.Shipping.Features.Queries.CalculateShippingCost;
using Application.Shipping.Features.Queries.GetAvailableShippings;
using Application.Shipping.Features.Queries.GetAvailableShippingsForVariants;
using Application.Shipping.Features.Queries.GetShippingQuotes;
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
using SharedKernel.Results;

namespace Tests.Presentation.Shipping.Endpoints;

public class CheckoutShippingControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly CheckoutShippingController _controller;

    public CheckoutShippingControllerTests()
    {
        _controller = new CheckoutShippingController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetAvailableShippings_WithPositiveAmount_SendsQuery_AndReturnsOk()
    {
        // Arrange
        IReadOnlyList<AvailableShippingDto> expected =
            [new AvailableShippingDto { Id = Guid.NewGuid(), Name = "Post", Cost = 50000 }];

        _mediator.Send(Arg.Any<GetAvailableShippingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<AvailableShippingDto>>.Success(expected));

        // Act
        var result = await _controller.GetAvailableShippings(100000);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetAvailableShippingsQuery>(q => q.OrderAmount == 100000),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAvailableShippings_WithNegativeAmount_ClampsToZero()
    {
        // Arrange
        IReadOnlyList<AvailableShippingDto> expected = [];

        _mediator.Send(Arg.Any<GetAvailableShippingsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<AvailableShippingDto>>.Success(expected));

        // Act
        var result = await _controller.GetAvailableShippings(-50);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetAvailableShippingsQuery>(q => q.OrderAmount == 0m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CalculateShippingCost_WithValidInput_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var shippingId = Guid.NewGuid();
        var expected = new ShippingCostResultDto { ShippingId = shippingId, Cost = 50000 };

        _mediator.Send(Arg.Any<CalculateShippingCostQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ShippingCostResultDto>.Success(expected));

        // Act
        var result = await _controller.CalculateShippingCost(shippingId, 100000, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<CalculateShippingCostQuery>(q => q.ShippingId == shippingId && q.OrderAmount == 100000),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetAvailableShippingsForVariants_WithIds_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        IReadOnlyList<AvailableShippingDto> expected =
            [new AvailableShippingDto { Id = Guid.NewGuid(), Name = "Post" }];

        _mediator.Send(Arg.Any<GetAvailableShippingsForVariantsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<AvailableShippingDto>>.Success(expected));

        // Act
        var result = await _controller.GetAvailableShippingsForVariants([variantId], CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetAvailableShippingsForVariantsQuery>(q => q.VariantIds.Contains(variantId)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetShippingQuotes_WithNullQuery_ReturnsEmptyOk_WithoutMediatorCall()
    {
        // Act
        var result = await _controller.GetShippingQuotes(null!, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<IReadOnlyList<AvailableShippingDto>>>();
        body.Data.ShouldBeEmpty();
        await _mediator.DidNotReceiveWithAnyArgs().Send(Arg.Any<GetShippingQuotesQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CheckoutShippingController_HasAuthorizeAttribute()
    {
        typeof(CheckoutShippingController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void CheckoutShippingController_HasRouteAttribute()
    {
        var routeAttr = typeof(CheckoutShippingController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/checkout/shipping");
    }

    [Theory]
    [InlineData(nameof(CheckoutShippingController.GetAvailableShippings), "available")]
    [InlineData(nameof(CheckoutShippingController.CalculateShippingCost), "cost")]
    [InlineData(nameof(CheckoutShippingController.GetAvailableShippingsForVariants), "available")]
    [InlineData(nameof(CheckoutShippingController.GetShippingQuotes), "quotes")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string expectedTemplate)
    {
        // Note: GetAvailableShippings (GET) and GetAvailableShippingsForVariants (POST)
        // share the "available" template with different HTTP methods.
        var methods = typeof(CheckoutShippingController).GetMethods()
            .Where(m => m.Name == methodName)
            .ToList();

        methods.Count.ShouldBeGreaterThan(0);
        methods.SelectMany(m => m.GetCustomAttributes(false).OfType<HttpMethodAttribute>())
            .Select(a => a.Template)
            .ShouldContain(expectedTemplate);
    }
}
