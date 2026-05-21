using Application.Category.Features.Queries.GetCategory;
using Application.Category.Features.Queries.GetCategoryProducts;
using Application.Category.Features.Queries.GetCategoryTree;
using Application.Category.Features.Queries.GetPublicCategories;
using Application.Category.Features.Shared;

namespace Presentation.Category.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/categories")]
public sealed class CategoryController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<CategoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetPublicCategoriesQuery(search, page, pageSize);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("tree")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<CategoryTreeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryHierarchy()
    {
        var query = new GetCategoryTreeQuery();
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CategoryDetailDto?>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        var query = new GetCategoryQuery(id);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}/products")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<CategoryProductItemDto?>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoryProducts(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetCategoryProductsQuery(id, true, page, pageSize);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }
}