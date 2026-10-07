using Application.Inventory.Features.Queries.GetBatchVariantAvailability;
using Application.Inventory.Features.Queries.GetVariantAvailability;
using Application.Inventory.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Inventory.Endpoints;
using Presentation.Inventory.Requests;
using SharedKernel.Results;

namespace Tests.Presentation.Inventory.Endpoints;

public class InventoryControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly InventoryController _controller;

    public InventoryControllerTests()
    {
        _controller = new InventoryController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetVariantAvailability_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var expected = new VariantAvailabilityDto { VariantId = variantId, IsAvailable = true, AvailableQuantity = 10 };

        _mediator.Send(Arg.Is<GetVariantAvailabilityQuery>(q => q.VariantId == variantId), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<VariantAvailabilityDto>.Success(expected));

        // Act
        var result = await _controller.GetVariantAvailability(variantId);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<VariantAvailabilityDto>>();
        body.Success.ShouldBeTrue();
        body.Data!.VariantId.ShouldBe(variantId);
    }

    [Fact]
    public async Task GetBatchAvailability_WithIds_SendsQuery_AndReturnsCreated()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var request = new BatchAvailabilityRequest([variantId]);
        IReadOnlyList<VariantAvailabilityDto> expected =
            [new VariantAvailabilityDto { VariantId = variantId, IsAvailable = true }];

        _mediator.Send(Arg.Any<GetBatchVariantAvailabilityQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<VariantAvailabilityDto>>.Success(expected));

        // Act
        var result = await _controller.GetBatchAvailability(request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<IReadOnlyList<VariantAvailabilityDto>>>();
        body.Success.ShouldBeTrue();
        body.Data!.Count.ShouldBe(1);
        await _mediator.Received(1).Send(
            Arg.Is<GetBatchVariantAvailabilityQuery>(q => q.VariantIds.Contains(variantId)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void InventoryController_AllowsAnonymous()
    {
        typeof(InventoryController).GetCustomAttributes(typeof(AllowAnonymousAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void InventoryController_HasRouteAttribute()
    {
        var routeAttr = typeof(InventoryController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/inventory");
    }

    [Theory]
    [InlineData(nameof(InventoryController.GetVariantAvailability), "availability/{variantId:guid}")]
    [InlineData(nameof(InventoryController.GetBatchAvailability), "availability/batch")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string expectedTemplate)
    {
        var method = typeof(InventoryController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
