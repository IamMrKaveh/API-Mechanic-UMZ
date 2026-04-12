using Application.Category.Features.Commands.CreateCategory;
using Application.Category.Features.Commands.DeleteCategory;
using Application.Category.Features.Commands.ReorderCategories;
using Application.Category.Features.Commands.UpdateCategory;
using Application.Category.Features.Queries.GetAdminCategories;
using Application.Category.Features.Queries.GetCategoryWithBrands;
using MapsterMapper;
using Presentation.Category.Requests;

namespace Presentation.Category.Endpoints;

[Route("api/admin/categories")]
[ApiController]
[Authorize(Roles = "Admin")]
public sealed class AdminCategoryController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    public async Task<IActionResult> GetCategories(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var result = await Mediator.Send(
            new GetAdminCategoriesQuery(search, isActive, includeDeleted, page, pageSize));
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCategory(Guid id)
    {
        var result = await Mediator.Send(new GetCategoryWithBrandsQuery(id));
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var command = new CreateCategoryCommand(
            request.Name,
            request.Slug,
            request.Description,
            request.SortOrder);

        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        var command = new UpdateCategoryCommand(
            id,
            request.Name,
            request.IsActive,
            request.Slug,
            request.Description,
            request.SortOrder,
            request.RowVersion);

        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var result = await Mediator.Send(new DeleteCategoryCommand(id));
        return ToActionResult(result);
    }

    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderCategories([FromBody] ReorderCategoriesRequest request)
    {
        var items = request.Items
            .Select(x => (x.CategoryId, x.SortOrder))
            .ToList<(Guid Id, int SortOrder)>();

        var command = new ReorderCategoriesCommand(items);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }
}