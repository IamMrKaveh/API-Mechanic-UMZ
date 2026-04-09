using Application.Category.Features.Queries.GetCategory;
using Application.Category.Features.Queries.GetCategoryProducts;
using Application.Category.Features.Queries.GetCategoryTree;
using Application.Category.Features.Queries.GetPublicCategories;

namespace Presentation.Category.Endpoints;

[Route("api/[controller]")]
[ApiController]
public class CategoryController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("hierarchy")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryHierarchy()
    {
        var query = new GetCategoryTreeQuery();
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories()
    {
        var query = new GetPublicCategoriesQuery();
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryById(
        Guid id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetCategoryQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}/products")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryProducts(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetCategoryProductsQuery(id, true, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}