namespace MainApi.Controllers.Product;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CategoryController : BaseApiController
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoryController> _logger;

    public CategoryController(
        ICategoryService categoryService,
        ILogger<CategoryController> logger,
        IConfiguration configuration) : base(configuration)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetCategories([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var (categories, totalItems) = await _categoryService.GetCategoriesAsync(search, page, pageSize);

            return Ok(new
            {
                Items = categories,
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories.");
            return StatusCode(500, "An error occurred while retrieving categories.");
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetCategory(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (id <= 0) return BadRequest("Invalid category ID.");
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        try
        {
            var category = await _categoryService.GetCategoryByIdAsync(id, page, pageSize);
            if (category == null)
            {
                return NotFound("Category not found.");
            }
            return Ok(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving category with ID {CategoryId}", id);
            return StatusCode(500, "An error occurred while retrieving the category.");
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<object>> CreateCategory([FromForm] CategoryDto categoryDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var createdCategory = await _categoryService.CreateCategoryAsync(categoryDto);
            return CreatedAtAction(nameof(GetCategory), new { id = (createdCategory.GetType().GetProperty("Id")?.GetValue(createdCategory, null)) }, createdCategory);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category.");
            return StatusCode(500, "An error occurred while creating the category.");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(int id, [FromForm] CategoryDto categoryDto)
    {
        if (id <= 0) return BadRequest("Invalid category ID.");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var (success, errorMessage) = await _categoryService.UpdateCategoryAsync(id, categoryDto);
            if (success) return NoContent();

            return errorMessage switch
            {
                "Category not found." => NotFound(new { message = errorMessage }),
                _ => BadRequest(new { message = errorMessage })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category with ID {CategoryId}", id);
            return StatusCode(500, "An error occurred while updating the category.");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        if (id <= 0) return BadRequest("Invalid category ID.");

        try
        {
            var (success, errorMessage) = await _categoryService.DeleteCategoryAsync(id);
            if (success) return NoContent();

            return errorMessage switch
            {
                "Category not found." => NotFound(new { message = errorMessage }),
                _ => BadRequest(new { message = errorMessage })
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category with ID {CategoryId}", id);
            return StatusCode(500, "An error occurred while deleting the category.");
        }
    }
}