using Application.Payment.Features.Queries.GetActivePaymentMethods;
using Application.Payment.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Payment.Endpoints;
using SharedKernel.Results;

namespace Tests.Presentation.Payment.Endpoints;

public class PaymentMethodsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly PaymentMethodsController _controller;

    public PaymentMethodsControllerTests()
    {
        _controller = new PaymentMethodsController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetActivePaymentMethods_WithOrderAmount_SendsQuery_AndReturnsOk()
    {
        // Arrange
        IReadOnlyList<AvailablePaymentMethodDto> expected =
            [new AvailablePaymentMethodDto { Id = Guid.NewGuid(), Name = "ZarinPal", Code = "zarinpal" }];

        _mediator.Send(Arg.Any<GetActivePaymentMethodsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<AvailablePaymentMethodDto>>.Success(expected));

        // Act
        var result = await _controller.GetActivePaymentMethods(50000);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<IReadOnlyList<AvailablePaymentMethodDto>>>();
        body.Success.ShouldBeTrue();
        body.Data!.Count.ShouldBe(1);
        await _mediator.Received(1).Send(
            Arg.Is<GetActivePaymentMethodsQuery>(q => q.OrderAmount == 50000),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetActivePaymentMethods_WithDefaultAmount_SendsQuery_AndReturnsOk()
    {
        // Arrange
        IReadOnlyList<AvailablePaymentMethodDto> expected = [];

        _mediator.Send(Arg.Any<GetActivePaymentMethodsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<AvailablePaymentMethodDto>>.Success(expected));

        // Act
        var result = await _controller.GetActivePaymentMethods();

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetActivePaymentMethodsQuery>(q => q.OrderAmount == 0m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void PaymentMethodsController_AllowsAnonymous()
    {
        typeof(PaymentMethodsController).GetCustomAttributes(typeof(AllowAnonymousAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void PaymentMethodsController_HasRouteAttribute()
    {
        var routeAttr = typeof(PaymentMethodsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/payment-methods");
    }
}
