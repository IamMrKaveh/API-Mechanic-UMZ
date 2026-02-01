using Application.Common.Interfaces.Category;

namespace MainApi.Controllers.Category;

[Route("api/[controller]")]
[ApiController]
public class CategoryGroupController : ControllerBase
{
    private readonly ICategoryGroupService _categoryGroupService;
    private readonly ILogger<CategoryGroupController> _logger;

    public CategoryGroupController(ICategoryGroupService categoryGroupService, ILogger<CategoryGroupController> logger)
    {
        _categoryGroupService = categoryGroupService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryGroups([FromQuery] int? categoryId, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var result = await _categoryGroupService.GetPagedAsync(categoryId, search, page, pageSize);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryGroup(int id)
    {
        if (id <= 0) return BadRequest("Invalid category group ID.");

        var result = await _categoryGroupService.GetByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(new { message = result.Error });
        }

        return Ok(result.Data);
    }
}