using Application.Category.Features.Commands.CreateCategory;
using Application.Category.Features.Commands.DeleteCategory;
using Application.Category.Features.Commands.ReorderCategories;
using Application.Category.Features.Commands.UpdateCategory;
using Application.Category.Features.Queries.GetAdminCategories;
using Application.Category.Features.Queries.GetCategoryWithGroups;
using Presentation.Category.Requests;

namespace Presentation.Category.Endpoints;

[Route("api/admin/categories")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminCategoryController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetCategories(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetAdminCategoriesQuery(search, isActive, includeDeleted, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategory(Guid id)
    {
        var query = new GetCategoryWithBrandsQuery(id);
        var result = await _mediator.Send(query);
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

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
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

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var command = new DeleteCategoryCommand(id);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderCategories([FromBody] ICollection<(Guid Id, int SortOrder)> request)
    {
        var command = new ReorderCategoriesCommand(request);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}