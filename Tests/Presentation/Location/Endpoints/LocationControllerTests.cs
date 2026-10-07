using Application.Location.Features.Queries.GetCities;
using Application.Location.Features.Queries.GetStates;
using Application.Location.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Location.Endpoints;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Location.Endpoints;

public class LocationControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly LocationController _controller;

    public LocationControllerTests()
    {
        _controller = new LocationController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetStates_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var paged = new PaginatedResult<ProvinceDto>
        {
            Items = [new ProvinceDto(1, "Tehran", "THR")],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetStatesQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<ProvinceDto>>.Success(paged));

        // Act
        var result = await _controller.GetStates();

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<ProvinceDto>>>();
        body.Success.ShouldBeTrue();
        body.Data!.Items.ShouldHaveSingleItem();
        await _mediator.Received(1).Send(Arg.Any<GetStatesQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCities_WithStateId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        IEnumerable<CityDto> expected = [new CityDto(1, "Tehran", "Tehran", 1)];

        _mediator.Send(Arg.Is<GetCitiesQuery>(q => q.StateId == 1), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IEnumerable<CityDto>>.Success(expected));

        // Act
        var result = await _controller.GetCities(1);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<IEnumerable<CityDto>>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldNotBeNull();
    }

    [Fact]
    public void LocationController_AllowsAnonymous()
    {
        typeof(LocationController).GetCustomAttributes(typeof(AllowAnonymousAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void LocationController_HasRouteAttribute()
    {
        var routeAttr = typeof(LocationController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/locations");
    }

    [Theory]
    [InlineData(nameof(LocationController.GetStates), "states")]
    [InlineData(nameof(LocationController.GetCities), "cities")]
    public void Actions_HaveExpectedHttpGetTemplate(string methodName, string expectedTemplate)
    {
        var method = typeof(LocationController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var httpGet = method!.GetCustomAttributes(typeof(HttpGetAttribute), false)
            .OfType<HttpGetAttribute>()
            .SingleOrDefault();
        httpGet.ShouldNotBeNull();
        httpGet!.Template.ShouldBe(expectedTemplate);
    }
}
