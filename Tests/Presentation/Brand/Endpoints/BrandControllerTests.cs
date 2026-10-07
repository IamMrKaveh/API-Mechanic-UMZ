using Application.Brand.Features.Queries.GetBrand;
using Application.Brand.Features.Queries.GetPublicBrands;
using Application.Brand.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Brand.Endpoints;
using Presentation.Brand.Requests;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using SharedKernel.Results;

namespace Tests.Presentation.Brand.Endpoints;

public class BrandControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly BrandController _controller;

    public BrandControllerTests()
    {
        _controller = new BrandController(_mediator, _mapper);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetBrands_MapsRequestToQuery_AndReturnsOk()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var request = new GetPublicBrandsRequest(categoryId);
        var query = new GetPublicBrandsQuery(categoryId);
        IReadOnlyList<BrandListItemDto> expected = [new BrandListItemDto { Id = Guid.NewGuid(), Name = "Brand1" }];

        _mapper.Map<GetPublicBrandsQuery>(request).Returns(query);
        _mediator.Send(Arg.Any<GetPublicBrandsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<BrandListItemDto>>.Success(expected));

        // Act
        var result = await _controller.GetBrands(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<IReadOnlyList<BrandListItemDto>>>();
        body.Success.ShouldBeTrue();
        body.Data.ShouldNotBeNull();
        body.Data!.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetBrand_WithValidId_SendsQueryWithId_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new BrandDetailDto { Id = id, Name = "Brand1" };

        _mediator.Send(Arg.Is<GetBrandQuery>(q => q.Id == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<BrandDetailDto>.Success(expected));

        // Act
        var result = await _controller.GetBrand(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<BrandDetailDto>>();
        body.Success.ShouldBeTrue();
        body.Data!.Id.ShouldBe(id);
    }

    [Fact]
    public async Task GetBrand_WhenNotFound_MapsToNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<GetBrandQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<BrandDetailDto>.NotFound());

        // Act
        var result = await _controller.GetBrand(id, CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void BrandController_HasRouteAttribute()
    {
        var routeAttr = typeof(BrandController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/brands");
    }

    [Fact]
    public void GetBrands_AllowsAnonymous()
    {
        var method = typeof(BrandController).GetMethod(nameof(BrandController.GetBrands));
        method.ShouldNotBeNull();
        method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), false).Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GetBrand_AllowsAnonymous()
    {
        var method = typeof(BrandController).GetMethod(nameof(BrandController.GetBrand));
        method.ShouldNotBeNull();
        method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), false).Length.ShouldBeGreaterThan(0);
    }
}
