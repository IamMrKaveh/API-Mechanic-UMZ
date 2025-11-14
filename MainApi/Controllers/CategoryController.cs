namespace MainApi.Controllers;

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

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCategory([FromForm] CategoryCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _categoryService.CreateCategoryAsync(dto);

        if (!result.Success || result.Data == null)
        {
            return Conflict(new { message = result.Error });
        }

        return CreatedAtAction(nameof(GetCategory), new { id = result.Data.Id }, result.Data);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(int id, [FromForm] CategoryUpdateDto dto)
    {
        if (id <= 0) return BadRequest("Invalid category ID.");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _categoryService.UpdateCategoryAsync(id, dto);

        if (result.Success) return NoContent();

        return result.Error switch
        {
            "Category not found." => NotFound(new { message = result.Error }),
            "The record you attempted to edit was modified by another user. Please reload and try again." => Conflict(new { message = result.Error }),
            _ => BadRequest(new { message = result.Error })
        };
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        if (id <= 0) return BadRequest("Invalid category ID.");

        var result = await _categoryService.DeleteCategoryAsync(id);

        if (result.Success) return NoContent();

        return result.Error switch
        {
            "Category not found." => NotFound(new { message = result.Error }),
            _ => BadRequest(new { message = result.Error })
        };
    }
}