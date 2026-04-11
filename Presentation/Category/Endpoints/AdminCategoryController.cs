using Application.Category.Features.Commands.CreateCategory;
using Application.Category.Features.Commands.DeleteCategory;
using Application.Category.Features.Commands.ReorderCategories;
using Application.Category.Features.Commands.UpdateCategory;
using Application.Category.Features.Queries.GetAdminCategories;
using Application.Category.Features.Queries.GetCategoryWithGroups;
using Application.Category.Features.Shared;
using MapsterMapper;

namespace Presentation.Category.Endpoints;

[Route("api/admin/categories")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminCategoryController(IMediator mediator, IMapper mapper) : BaseApiController(mediator)
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategory(Guid id)
    {
        var result = await Mediator.Send(new GetCategoryWithBrandsQuery(id));
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        var command = mapper.Map<CreateCategoryCommand>(dto);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryDto dto)
    {
        var command = mapper.Map<UpdateCategoryCommand>(dto) with { Id = id };
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var result = await Mediator.Send(new DeleteCategoryCommand(id));
        return ToActionResult(result);
    }

    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderCategories([FromBody] ReorderCategoriesDto dto)
    {
        var items = dto.Items.Select(x => ((Guid)x.CategoryId, x.SortOrder)).ToList<(Guid Id, int SortOrder)>();
        var command = new ReorderCategoriesCommand(items);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }
}