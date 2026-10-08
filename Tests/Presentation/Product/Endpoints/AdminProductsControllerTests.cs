using Application.Product.Features.Commands.ActivateProduct;
using Application.Product.Features.Commands.BulkUpdatePrices;
using Application.Product.Features.Commands.CreateProduct;
using Application.Product.Features.Commands.DeactivateProduct;
using Application.Product.Features.Commands.DeleteProduct;
using Application.Product.Features.Commands.RestoreProduct;
using Application.Product.Features.Commands.UpdateProduct;
using Application.Product.Features.Commands.UpdateProductDetails;
using Application.Product.Features.Queries.GetAdminProduct;
using Application.Product.Features.Queries.GetAdminProductDetail;
using Application.Product.Features.Queries.GetAdminProducts;
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

public class AdminProductsControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AdminProductsController _controller;

    public AdminProductsControllerTests()
    {
        _controller = new AdminProductsController(_mediator, Substitute.For<IMapper>());

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetProducts_WithFilters_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var request = new GetAdminProductsRequest(Page: 1, PageSize: 10, CategoryId: categoryId);
        var paged = new PaginatedResult<ProductListItemDto>
        {
            Items = [new ProductListItemDto { Id = Guid.NewGuid(), Name = "P1" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetAdminProductsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<ProductListItemDto>>.Success(paged));

        // Act
        var result = await _controller.GetProducts(request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<ProductListItemDto>>>();
        body.Data!.Items.ShouldHaveSingleItem();
        await _mediator.Received(1).Send(
            Arg.Is<GetAdminProductsQuery>(q => q.CategoryId == categoryId && q.Page == 1 && q.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProduct_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new AdminProductDetailDto { Id = id, Name = "P1" };

        _mediator.Send(Arg.Is<GetAdminProductQuery>(q => q.ProductId == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminProductDetailDto?>.Success(expected));

        // Act
        var result = await _controller.GetProduct(id);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task GetProductDetail_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new AdminProductDetailDto { Id = id, Name = "P1" };

        _mediator.Send(Arg.Is<GetAdminProductDetailQuery>(q => q.ProductId == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminProductDetailDto?>.Success(expected));

        // Act
        var result = await _controller.GetProductDetail(id);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task CreateProduct_WithValidRequest_MapsToCommand_AndReturnsCreated()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var request = new CreateProductRequest("P1", categoryId, brandId);
        var expected = new ProductDetailDto { Id = Guid.NewGuid(), Name = "P1" };

        _mediator.Send(Arg.Any<CreateProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ProductDetailDto>.Success(expected));

        // Act
        var result = await _controller.CreateProduct(request);

        // Assert
        var created = result.ShouldBeOfType<ObjectResult>();
        created.StatusCode.ShouldBe(StatusCodes.Status201Created);
        await _mediator.Received(1).Send(
            Arg.Is<CreateProductCommand>(c => c.CategoryId == categoryId && c.BrandId == brandId && c.Name == "P1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProduct_WithValidRequest_MapsToCommandWithRouteId_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var request = new UpdateProductRequest(Guid.NewGuid(), categoryId, brandId, "P1", "p1", null, true, false, "rv");
        var expected = new ProductDetailDto { Id = id, Name = "P1" };

        _mediator.Send(Arg.Any<UpdateProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<ProductDetailDto>.Success(expected));

        // Act
        var result = await _controller.UpdateProduct(id, request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateProductCommand>(c => c.Id == id && c.Name == "P1" && c.RowVersion == "rv"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BulkUpdatePrices_WithItems_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var request = new BulkUpdatePricesRequest(
            [new VariantPriceUpdateInput(Guid.NewGuid(), Guid.NewGuid(), 100, 120)]);

        _mediator.Send(Arg.Any<BulkUpdatePricesCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.BulkUpdatePrices(request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<BulkUpdatePricesCommand>(c => c.Updates.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateProductDetails_WithValidRequest_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var brandId = Guid.NewGuid();
        var request = new UpdateProductDetailsRequest(Guid.NewGuid(), "P1", "Desc", brandId, true, "SKU1", "rv");

        _mediator.Send(Arg.Any<UpdateProductDetailsCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.UpdateProductDetails(id, request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateProductDetailsCommand>(c => c.ProductId == id && c.Name == "P1" && c.BrandId == brandId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteProduct_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DeleteProduct(id);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeleteProductCommand>(c => c.ProductId == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ActivateProduct_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<ActivateProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ActivateProduct(id);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<ActivateProductCommand>(c => c.ProductId == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeactivateProduct_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeactivateProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DeactivateProduct(id);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeactivateProductCommand>(c => c.ProductId == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RestoreProduct_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<RestoreProductCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.RestoreProduct(id);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<RestoreProductCommand>(c => c.ProductId == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetProduct_WhenNotFound_MapsToNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<GetAdminProductQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<AdminProductDetailDto?>.NotFound());

        // Act
        var result = await _controller.GetProduct(id);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void AdminProductsController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminProductsController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminProductsController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminProductsController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/products");
    }

    [Theory]
    [InlineData(nameof(AdminProductsController.GetProducts), null)]
    [InlineData(nameof(AdminProductsController.GetProduct), "{id:guid}")]
    [InlineData(nameof(AdminProductsController.GetProductDetail), "{id:guid}/details")]
    [InlineData(nameof(AdminProductsController.CreateProduct), null)]
    [InlineData(nameof(AdminProductsController.UpdateProduct), "{id:guid}")]
    [InlineData(nameof(AdminProductsController.BulkUpdatePrices), "prices/bulk")]
    [InlineData(nameof(AdminProductsController.UpdateProductDetails), "{id:guid}/details")]
    [InlineData(nameof(AdminProductsController.DeleteProduct), "{id:guid}")]
    [InlineData(nameof(AdminProductsController.ActivateProduct), "{id:guid}/activate")]
    [InlineData(nameof(AdminProductsController.DeactivateProduct), "{id:guid}/deactivate")]
    [InlineData(nameof(AdminProductsController.RestoreProduct), "{id:guid}/restore")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AdminProductsController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
