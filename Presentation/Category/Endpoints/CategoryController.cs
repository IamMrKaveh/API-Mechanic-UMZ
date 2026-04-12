using Application.Category.Features.Queries.GetCategory;
using Application.Category.Features.Queries.GetCategoryProducts;
using Application.Category.Features.Queries.GetCategoryTree;
using Application.Category.Features.Queries.GetPublicCategories;

namespace Presentation.Category.Endpoints;

[Route("api/[controller]")]
[ApiController]
public sealed class CategoryController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet("hierarchy")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryHierarchy()
    {
        var result = await Mediator.Send(new GetCategoryTreeQuery());
        return ToActionResult(result);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await Mediator.Send(new GetPublicCategoriesQuery(search, page, pageSize));
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        var result = await Mediator.Send(new GetCategoryQuery(id));
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}/products")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryProducts(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await Mediator.Send(new GetCategoryProductsQuery(id, true, page, pageSize));
        return ToActionResult(result);
    }
}