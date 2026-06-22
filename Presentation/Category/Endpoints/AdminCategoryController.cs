using Application.Category.Features.Commands.CreateCategory;
using Application.Category.Features.Commands.DeleteCategory;
using Application.Category.Features.Commands.ReorderCategories;
using Application.Category.Features.Commands.UpdateCategory;
using Application.Category.Features.Queries.GetAdminCategories;
using Application.Category.Features.Queries.GetCategoryWithBrands;
using Application.Category.Features.Shared;
using Presentation.Category.Requests;

namespace Presentation.Category.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/admin/categories")]
[Authorize(Roles = "Admin")]
public sealed class AdminCategoryController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<CategoryListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategories(
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetAdminCategoriesQuery(search, isActive, includeDeleted, page, pageSize);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryWithBrandsDto?>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategory(Guid id)
    {
        var query = new GetCategoryWithBrandsQuery(id);
        var result = await Mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var command = new CreateCategoryCommand(
            request.Name,
            request.Slug,
            request.Description,
            request.SortOrder);

        var result = await Mediator.Send(command);
        return ToCreatedActionResult(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), StatusCodes.Status409Conflict)]
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
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var command = new DeleteCategoryCommand(id);
        var result = await Mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPatch("order")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
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