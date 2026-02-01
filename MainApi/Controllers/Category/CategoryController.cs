using Application.Common.Interfaces.Category;

namespace MainApi.Controllers.Category;

[Route("api/[controller]")]
[ApiController]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    [HttpGet("hierarchy")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryHierarchy()
    {
        var result = await _categoryService.GetCategoryHierarchyAsync();
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var result = await _categoryService.GetCategoriesAsync(search, page, pageSize);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategory(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (id <= 0) return BadRequest("Invalid category ID.");
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var result = await _categoryService.GetCategoryByIdAsync(id, page, pageSize);

        if (!result.Success)
        {
            return NotFound(new { message = result.Error });
        }

        return Ok(result.Data);
    }
}