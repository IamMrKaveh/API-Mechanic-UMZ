using Application.Category.Features.Commands.CreateCategory;
using Application.Category.Features.Commands.DeleteCategory;
using Application.Category.Features.Commands.ReorderCategories;
using Application.Category.Features.Commands.UpdateCategory;
using Application.Category.Features.Queries.GetAdminCategories;
using Application.Category.Features.Queries.GetCategoryWithBrands;
using Application.Category.Features.Shared;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Presentation.Base.Responses;
using Presentation.Category.Endpoints;
using Presentation.Category.Requests;
using Presentation.Common.Interfaces;
using Presentation.Common.Mappers;
using SharedKernel.Models;
using SharedKernel.Results;

namespace Tests.Presentation.Category.Endpoints;

public class AdminCategoryControllerTests
{
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly AdminCategoryController _controller;

    public AdminCategoryControllerTests()
    {
        _controller = new AdminCategoryController(_mediator, Substitute.For<IMapper>());

        var services = new ServiceCollection();
        services.AddSingleton<IHttpResultMapper>(new HttpResultMapper());
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
    }

    [Fact]
    public async Task GetCategories_WithDefaults_SendsQuery_AndReturnsOk()
    {
        // Arrange
        var paged = new PaginatedResult<CategoryListItemDto>
        {
            Items = [new CategoryListItemDto { Id = Guid.NewGuid(), Name = "Cat1" }],
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mediator.Send(Arg.Any<GetAdminCategoriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<PaginatedResult<CategoryListItemDto>>.Success(paged));

        // Act
        var result = await _controller.GetCategories();

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        var body = ok.Value.ShouldBeOfType<ApiResponse<PaginatedResult<CategoryListItemDto>>>();
        body.Success.ShouldBeTrue();
        body.Data!.Items.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task GetCategory_WithValidId_SendsQueryWithCategoryId_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var expected = new CategoryWithBrandsDto { Id = id, Name = "Cat1" };

        _mediator.Send(Arg.Is<GetCategoryWithBrandsQuery>(q => q.CategoryId == id), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CategoryWithBrandsDto?>.Success(expected));

        // Act
        var result = await _controller.GetCategory(id);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task CreateCategory_WithValidRequest_MapsToCommand_AndReturnsCreated()
    {
        // Arrange
        var request = new CreateCategoryRequest("Cat1", "cat-1", "Description", 1);
        var expected = new CategoryDto { Id = Guid.NewGuid(), Name = "Cat1" };

        _mediator.Send(Arg.Any<CreateCategoryCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CategoryDto>.Success(expected));

        // Act
        var result = await _controller.CreateCategory(request);

        // Assert
        var created = result.ShouldBeOfType<ObjectResult>();
        created.StatusCode.ShouldBe(StatusCodes.Status201Created);
        await _mediator.Received(1).Send(
            Arg.Is<CreateCategoryCommand>(c => c.CategoryName == "Cat1" && c.Slug == "cat-1" && c.SortOrder == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateCategory_WithValidRequest_MapsToCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new UpdateCategoryRequest("Cat1", "cat-1", "Description", 2, true, "rv");
        var expected = new CategoryDto { Id = id, Name = "Cat1" };

        _mediator.Send(Arg.Any<UpdateCategoryCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult<CategoryDto>.Success(expected));

        // Act
        var result = await _controller.UpdateCategory(id, request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<UpdateCategoryCommand>(c => c.Id == id && c.Name == "Cat1" && c.IsActive && c.RowVersion == "rv"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteCategory_WithValidId_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteCategoryCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.DeleteCategory(id);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(
            Arg.Is<DeleteCategoryCommand>(c => c.CategoryId == id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReorderCategories_SendsCommand_AndReturnsOk()
    {
        // Arrange
        var request = new ReorderCategoriesRequest([new CategoryOrderItemRequest(Guid.NewGuid(), 0)]);

        _mediator.Send(Arg.Any<ReorderCategoriesCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.Success());

        // Act
        var result = await _controller.ReorderCategories(request);

        // Assert
        var ok = result.ShouldBeOfType<OkObjectResult>();
        ok.StatusCode.ShouldBe(StatusCodes.Status200OK);
        await _mediator.Received(1).Send(Arg.Any<ReorderCategoriesCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteCategory_WhenNotFound_MapsToNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();

        _mediator.Send(Arg.Any<DeleteCategoryCommand>(), Arg.Any<CancellationToken>())
            .Returns(ServiceResult.NotFound());

        // Act
        var result = await _controller.DeleteCategory(id);

        // Assert
        var notFound = result.ShouldBeOfType<ObjectResult>();
        notFound.StatusCode.ShouldBe(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void AdminCategoryController_HasAuthorizeAttribute_WithAdminRole()
    {
        var authorizeAttr = typeof(AdminCategoryController).GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        authorizeAttr.ShouldNotBeNull();
        authorizeAttr!.Roles.ShouldBe("Admin");
    }

    [Fact]
    public void AdminCategoryController_HasRouteAttribute()
    {
        var routeAttr = typeof(AdminCategoryController).GetCustomAttributes(typeof(RouteAttribute), false)
            .OfType<RouteAttribute>()
            .SingleOrDefault();

        routeAttr.ShouldNotBeNull();
        routeAttr!.Template.ShouldBe("api/v{version:apiVersion}/admin/categories");
    }

    [Theory]
    [InlineData(nameof(AdminCategoryController.GetCategories), null)]
    [InlineData(nameof(AdminCategoryController.GetCategory), "{id:guid}")]
    [InlineData(nameof(AdminCategoryController.CreateCategory), null)]
    [InlineData(nameof(AdminCategoryController.UpdateCategory), "{id:guid}")]
    [InlineData(nameof(AdminCategoryController.DeleteCategory), "{id:guid}")]
    [InlineData(nameof(AdminCategoryController.ReorderCategories), "order")]
    public void Actions_HaveExpectedHttpTemplate(string methodName, string? expectedTemplate)
    {
        var method = typeof(AdminCategoryController).GetMethod(methodName);
        method.ShouldNotBeNull();
        var template = method!.GetCustomAttributes(false)
            .OfType<HttpMethodAttribute>()
            .SingleOrDefault()
            ?.Template;
        template.ShouldBe(expectedTemplate);
    }
}
