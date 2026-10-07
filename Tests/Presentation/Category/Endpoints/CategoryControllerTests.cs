using Application.Category.Features.Queries.GetCategory;
using Application.Category.Features.Queries.GetCategoryProducts;
using Application.Category.Features.Queries.GetCategoryTree;
using Application.Category.Features.Queries.GetPublicCategories;
using Application.Category.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Category.Endpoints;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Category.Endpoints;

public class CategoryControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly CategoryController _controller;

    public CategoryControllerTests()
    {
        _controller = new CategoryController(_mediator);

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetCategories_SendsQueryWithPaging_AndReturnsOk()
    {
        // Arrange
        var paged = new PaginatedResult<CategoryDto>
        {
            Items = [new CategoryDto { Id = Guid.NewGuid(), Name = "Cat1" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetPublicCategoriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<CategoryDto>>.Success(paged));

        // Act
        var result = await _controller.GetCategories("Cat", 1, 10);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<CategoryDto>>>();
        body.Success.ShouldBeTrue();
        body.Data!.Items.ShouldHaveSingleItem();
        await _mediator.Received(1).Send(
            Arg.Is<GetPublicCategoriesQuery>(q => q.Search == "Cat" && q.Page == 1 && q.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetCategoryHierarchy_SendsQuery_AndReturnsOk()
    {
        // Arrange
        IReadOnlyList<CategoryTreeDto> expected =
            [new CategoryTreeDto { Id = Guid.NewGuid(), Name = "Root" }];

        _mediator.Send(Arg.Any<GetCategoryTreeQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<IReadOnlyList<CategoryTreeDto>>.Success(expected));

        // Act
        var result = await _controller.GetCategoryHierarchy();

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<IReadOnlyList<CategoryTreeDto>>>();
        body.Success.ShouldBeTrue();
        body.Data!.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetCategoryById_WithValidId_SendsQueryWithId_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new CategoryDetailDto { Id = id, Name = "Cat1" };

        _mediator.Send(Arg.Is<GetCategoryQuery>(q => q.Id == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CategoryDetailDto>.Success(expected));

        // Act
        var result = await _controller.GetCategoryById(id);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<CategoryDetailDto?>>();
        body.Success.ShouldBeTrue();
    }

    [Fact]
    public async Task GetCategoryProducts_WithValidId_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var paged = new PaginatedResult<CategoryProductItemDto>
        {
            Items = [new CategoryProductItemDto { Id = Guid.NewGuid(), Name = "Product1" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetCategoryProductsQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<CategoryProductItemDto>>.Success(paged));

        // Act
        var result = await _controller.GetCategoryProducts(id, 1, 10);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<GetCategoryProductsQuery>(q => q.CategoryId == id && q.Page == 1 && q.PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void CategoryController_HasRouteAttribute()
    {
        var routeAttr = typeof(CategoryController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/categories");
    }

    [Theory]
    [InlineData(nameof(CategoryController.GetCategories))]
    [InlineData(nameof(CategoryController.GetCategoryHierarchy))]
    [InlineData(nameof(CategoryController.GetCategoryById))]
    [InlineData(nameof(CategoryController.GetCategoryProducts))]
    public void Actions_AllowAnonymous(string methodName)
    {
        var method = typeof(CategoryController).GetMethod(methodName);
        method.ShouldNotBeNull();
        method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), false).Length.ShouldBeGreaterThan(0);
    }

    [Theory]
    [InlineData(nameof(CategoryController.GetCategories), null)]
    [InlineData(nameof(CategoryController.GetCategoryHierarchy), "tree")]
    [InlineData(nameof(CategoryController.GetCategoryById), "{id:guid}")]
    [InlineData(nameof(CategoryController.GetCategoryProducts), "{id:guid}/products")]
    public void Actions_HaveExpectedHttpGetTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(CategoryController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var httpGet = method!.GetCustomAttributes(typeof(HttpGetAttribute), false)
            .OfType<HttpGetAttribute>()
            .SingleOrDefault();
        httpGet.ShouldNotBeNull();
        httpGet!.Template.ShouldBe(expectedTemplate);
    }
}
