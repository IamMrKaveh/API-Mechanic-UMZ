using Application.Common.Interfaces.Admin.Category;
using Application.DTOs.Category;

namespace MainApi.Controllers.Admin;

[Route("api/admin/categories")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminCategoryController : ControllerBase
{
    private readonly IAdminCategoryService _adminCategoryService;

    public AdminCategoryController(IAdminCategoryService adminCategoryService)
    {
        _adminCategoryService = adminCategoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;
        var result = await _adminCategoryService.GetCategoriesAsync(search, page, pageSize);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategory(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (id <= 0) return BadRequest("Invalid category ID.");
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var result = await _adminCategoryService.GetCategoryByIdAsync(id, page, pageSize);

        if (!result.Success)
        {
            return NotFound(new { message = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromForm] CategoryCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _adminCategoryService.CreateCategoryAsync(dto);
        if (!result.Success || result.Data == null)
        {
            return Conflict(new { message = result.Error });
        }
        return CreatedAtAction(nameof(GetCategory), new { id = result.Data.Id }, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromForm] CategoryUpdateDto dto)
    {
        if (id <= 0) return BadRequest("Invalid category ID.");
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _adminCategoryService.UpdateCategoryAsync(id, dto);
        if (result.Success) return NoContent();
        return result.Error switch
        {
            "Category not found." => NotFound(new { message = result.Error }),
            "The record you attempted to edit was modified by another user. Please reload and try again." => Conflict(new { message = result.Error }),
            _ => BadRequest(new { message = result.Error })
        };
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        if (id <= 0) return BadRequest("Invalid category ID.");
        var result = await _adminCategoryService.DeleteCategoryAsync(id);
        if (result.Success) return NoContent();
        return result.Error switch
        {
            "Category not found." => NotFound(new { message = result.Error }),
            _ => BadRequest(new { message = result.Error })
        };
    }
}