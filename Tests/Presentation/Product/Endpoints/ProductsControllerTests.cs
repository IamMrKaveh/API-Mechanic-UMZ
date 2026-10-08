using Application.Product.Features.Queries.GetProduct;
using Application.Product.Features.Queries.GetProductCatalog;
using Application.Product.Features.Queries.GetProductDetails;
using Application.Product.Features.Queries.GetProducts;
using Application.Product.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using Presentation.Product.Endpoints;
using Presentation.Product.Requests;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Product.Endpoints;

public class ProductsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IMapper _mapper = Substitute.For<IMapper>();
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _controller = new ProductsController(_mediator, _mapper);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetProducts_MapsRequestToQuery_AndReturnsOk()
    {
        // Arrange
        var request = new GetProductsRequest(Page: 1, PageSize: 10, Search: "phone");
        var query = new GetProductsQuery(null, null, "phone", null, false, 1, 10);
        var paged = new PaginatedResult<ProductListItemDto>
        {
            Items = [new ProductListItemDto { Id = Guid.NewGuid(), Name = "Phone" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mapper.Map<GetProductsQuery>(request).Returns(query);
        _mediator.Send(Arg.Any<GetProductsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<ProductListItemDto>>.Success(paged));

        // Act
        var result = await _controller.GetProducts(request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<ProductListItemDto>>>();
        body.Data!.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task GetProduct_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new ProductDetailDto { Id = id, Name = "Phone" };

        _mediator.Send(Arg.Is<GetProductQuery>(q => q.Id == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ProductDetailDto>.Success(expected));

        // Act
        var result = await _controller.GetProduct(id);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<ProductDetailDto>>();
        body.Data!.Id.ShouldBe(id);
    }

    [Fact]
    public async Task GetCatalog_WithDefaults_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var paged = new PaginatedResult<ProductCatalogItemDto>
        {
            Items = [new ProductCatalogItemDto { Id = Guid.NewGuid(), Name = "Phone" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetProductCatalogQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<ProductCatalogItemDto>>.Success(paged));

        // Act
        var result = await _controller.GetCatalog();

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<ProductCatalogItemDto>>>();
        body.Data!.Items.ShouldHaveSingleItem();
        await _mediator.Received(1).Send(
            Arg.Is<GetProductCatalogQuery>(q => q.Page == 1 && q.PageSize == 10 && q.HasDiscount == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetDiscountedProducts_ForcesHasDiscount_AndReturnsOk()
    {
        // Arrange
        var paged = new PaginatedResult<ProductCatalogItemDto>
        {
            Items = [new ProductCatalogItemDto { Id = Guid.NewGuid(), Name = "Phone" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 12
        };

        _mediator.Send(Arg.Any<GetProductCatalogQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<ProductCatalogItemDto>>.Success(paged));

        // Act
        var result = await _controller.GetDiscountedProducts();

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetProductCatalogQuery>(q => q.HasDiscount == true && q.PageSize == 12),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProductDetails_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new PublicProductDetailDto { Id = id, Name = "Phone" };

        _mediator.Send(Arg.Is<GetProductDetailsQuery>(q => q.ProductId == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PublicProductDetailDto?>.Success(expected));

        // Act
        var result = await _controller.GetProductDetails(id);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public void ProductsController_AllowsAnonymous()
    {
        typeof(ProductsController).GetCustomAttributes(typeof(AllowAnonymousAttribute), false)
            .Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void ProductsController_HasRouteAttribute()
    {
        var routeAttr = typeof(ProductsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/products");
    }

    [Theory]
    [InlineData(nameof(ProductsController.GetProducts), null)]
    [InlineData(nameof(ProductsController.GetProduct), "{id:guid}")]
    [InlineData(nameof(ProductsController.GetCatalog), "catalog")]
    [InlineData(nameof(ProductsController.GetDiscountedProducts), "discounted")]
    [InlineData(nameof(ProductsController.GetProductDetails), "{id:guid}/details")]
    public void Actions_HaveExpectedHttpGetTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(ProductsController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var httpGet = method!.GetCustomAttributes(typeof(HttpGetAttribute), false)
            .OfType<HttpGetAttribute>()
            .SingleOrDefault();
        httpGet.ShouldNotBeNull();
        httpGet!.Template.ShouldBe(expectedTemplate);
    }
}
