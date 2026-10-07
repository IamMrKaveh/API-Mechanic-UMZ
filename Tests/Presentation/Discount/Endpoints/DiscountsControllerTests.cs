using Application.Discount.Features.Commands.ApplyDiscount;
using Application.Discount.Features.Queries.ValidateDiscount;
using Application.Discount.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Discount.Endpoints;
using Presentation.Discount.Requests;
using SharedKernel.Results;

namespace Tests.Presentation.Discount.Endpoints;

public class DiscountsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly DiscountsController _controller;

    public DiscountsControllerTests()
    {
        _controller = new DiscountsController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task Validate_WithValidRequest_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var request = new ValidateDiscountRequest("SAVE10", 1000, "IRT");
        var expected = new DiscountValidationResult
        {
            Code = "SAVE10",
            IsValid = true,
            DiscountAmount = 100,
            FinalAmount = 900
        };

        _mediator.Send(Arg.Any<ValidateDiscountQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DiscountValidationResult>.Success(expected));

        // Act
        var result = await _controller.Validate(request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<DiscountValidationResult>>();
        body.Success.ShouldBeTrue();
        body.Data!.IsValid.ShouldBeTrue();
        await _mediator.Received(1).Send(
            Arg.Is<ValidateDiscountQuery>(q => q.Code == "SAVE10" && q.OrderAmount == 1000 && q.Currency == "IRT"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Apply_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var request = new ApplyDiscountRequest("SAVE10", orderId, 1000);
        var expected = new DiscountApplicationResult { IsSuccess = true, DiscountAmount = 100, FinalAmount = 900 };

        _mediator.Send(Arg.Any<ApplyDiscountCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DiscountApplicationResult>.Success(expected));

        // Act
        var result = await _controller.Apply(request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<DiscountApplicationResult>>();
        body.Success.ShouldBeTrue();
        body.Data!.DiscountAmount.ShouldBe(100);
        await _mediator.Received(1).Send(
            Arg.Is<ApplyDiscountCommand>(c => c.Code == "SAVE10" && c.OrderAmount == 1000 && c.OrderId == orderId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Validate_WhenInvalid_MapsToErrorStatus()
    {
        // Arrange
        var request = new ValidateDiscountRequest("EXPIRED", 1000, "IRT");

        _mediator.Send(Arg.Any<ValidateDiscountQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<DiscountValidationResult>.Failure("کد تخفیف معتبر نیست."));

        // Act
        var result = await _controller.Validate(request);

        // Assert
        var failure = result.ShouldBeOfType<ObjectResult>();
        failure.StatusCode.ShouldBe(StatusCodes.Status500InternalServerError);
    }

    [Fact]
    public void DiscountsController_HasRouteAttribute()
    {
        var routeAttr = typeof(DiscountsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/discounts");
    }

    [Fact]
    public void DiscountsController_HasAuthorizeAttribute()
    {
        typeof(DiscountsController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(nameof(DiscountsController.Validate), "validation")]
    [InlineData(nameof(DiscountsController.Apply), "application")]
    public void Actions_HaveExpectedHttpPostTemplate(string methodName, string expectedTemplate)
    {
        var method = typeof(DiscountsController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var httpPost = method!.GetCustomAttributes(typeof(HttpPostAttribute), false)
            .OfType<HttpPostAttribute>()
            .SingleOrDefault();
        httpPost.ShouldNotBeNull();
        httpPost!.Template.ShouldBe(expectedTemplate);
    }
}
