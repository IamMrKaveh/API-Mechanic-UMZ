using Application.Variant.Features.Commands.UpdateProductVariantShipping;
using Application.Variant.Features.Queries.GetVariantShipping;
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

public class AdminVariantShippingControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AdminVariantShippingController _controller;

    public AdminVariantShippingControllerTests()
    {
        _controller = new AdminVariantShippingController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetVariantShipping_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var expected = new VariantShippingInfoDto { VariantId = variantId, ShippingMultiplier = 1.5m };

        _mediator.Send(Arg.Is<GetVariantShippingQuery>(q => q.VariantId == variantId), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<VariantShippingInfoDto>.Success(expected));

        // Act
        var result = await _controller.GetVariantShipping(variantId, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<VariantShippingInfoDto>>();
        body.Data!.VariantId.ShouldBe(variantId);
    }

    [Fact]
    public async Task UpdateVariantShipping_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var shippingId = Guid.NewGuid();
        var request = new UpdateVariantShippingRequest(1.5m, 500, [shippingId]);

        _mediator.Send(Arg.Any<UpdateVariantShippingCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.UpdateVariantShipping(variantId, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateVariantShippingCommand>(c =>
                c.VariantId == variantId &&
                c.ShippingMultiplier == 1.5m &&
                c.WeightGrams == 500),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void AdminVariantShippingController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminVariantShippingController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminVariantShippingController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminVariantShippingController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/variants/shipping");
    }

    [Theory]
    [InlineData(nameof(AdminVariantShippingController.GetVariantShipping), "{variantId:guid}")]
    [InlineData(nameof(AdminVariantShippingController.UpdateVariantShipping), "{variantId:guid}")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string expectedTemplate)
    {
        var method = typeof(AdminVariantShippingController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
