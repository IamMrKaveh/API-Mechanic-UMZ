using Application.Brand.Features.Commands.DeleteBrand;
using Application.Brand.Features.Commands.MoveBrand;
using Application.Brand.Features.Commands.UpdateBrand;
using Application.Brand.Features.Queries.GetAdminBrands;
using Application.Brand.Features.Queries.GetBrandDetail;
using Application.Brand.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Brand.Endpoints;
using Presentation.Brand.Requests;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Brand.Endpoints;

public class AdminBrandControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly AdminBrandController _controller;

    public AdminBrandControllerTests()
    {
        _controller = new AdminBrandController(_mediator, _mapper);

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
        var request = new GetAdminBrandsRequest { Page = 1, PageSize = 10 };
        var query = new GetAdminBrandsQuery(null, null, null, false, 1, 10);
        var paged = new PaginatedResult<BrandListItemDto>
        {
            Items = [new BrandListItemDto { Id = Guid.NewGuid(), Name = "Brand1" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mapper.Map<GetAdminBrandsQuery>(request).Returns(query);
        _mediator.Send(Arg.Any<GetAdminBrandsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<BrandListItemDto>>.Success(paged));

        // Act
        var result = await _controller.GetBrands(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<BrandListItemDto>>>();
        body.Success.ShouldBeTrue();
        body.Data!.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task GetBrand_WithValidId_SendsQueryWithBrandId_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new BrandDetailDto { Id = id, Name = "Brand1" };

        _mediator.Send(Arg.Is<GetBrandDetailQuery>(q => q.BrandId == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<BrandDetailDto?>.Success(expected));

        // Act
        var result = await _controller.GetBrand(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task UpdateBrand_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateBrandRequest
        {
            CategoryId = Guid.NewGuid(),
            Name = "Updated",
            RowVersion = "rv"
        };
        var expected = new BrandDetailDto { Id = id, Name = "Updated" };

        _mediator.Send(Arg.Any<UpdateBrandCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<BrandDetailDto>.Success(expected));

        // Act
        var result = await _controller.UpdateBrand(id, request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateBrandCommand>(c => c.BrandId == id && c.Name == "Updated"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteBrand_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteBrandCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DeleteBrand(id, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeleteBrandCommand>(c => c.BrandId == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MoveBrand_MapsRequestToCommand_AndReturnsOk()
    {
        // Arrange
        var request = new MoveBrandRequest(Guid.NewGuid(), Guid.NewGuid());
        var command = new MoveBrandCommand(request.BrandId, request.TargetCategoryId);

        _mapper.Map<MoveBrandCommand>(request).Returns(command);
        _mediator.Send(Arg.Any<MoveBrandCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.MoveBrand(request, CancellationToken.None);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<MoveBrandCommand>(c => c.BrandId == request.BrandId && c.TargetCategoryId == request.TargetCategoryId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetBrand_WhenNotFound_MapsToNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<GetBrandDetailQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<BrandDetailDto?>.NotFound());

        // Act
        var result = await _controller.GetBrand(id, CancellationToken.None);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void AdminBrandController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminBrandController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminBrandController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminBrandController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/brands");
    }

    [Theory]
    [InlineData(nameof(AdminBrandController.GetBrands), null)]
    [InlineData(nameof(AdminBrandController.GetBrand), "{id:guid}")]
    [InlineData(nameof(AdminBrandController.CreateBrand), null)]
    [InlineData(nameof(AdminBrandController.UpdateBrand), "{id:guid}")]
    [InlineData(nameof(AdminBrandController.DeleteBrand), "{id:guid}")]
    [InlineData(nameof(AdminBrandController.MoveBrand), "move")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AdminBrandController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
