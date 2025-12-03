using Application.Common.Interfaces.Admin;

namespace MainApi.Controllers.Admin;

[Route("api/admin/category-groups")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminCategoryGroupController : ControllerBase
{
    private readonly IAdminCategoryGroupService _adminCategoryGroupService;

    public AdminCategoryGroupController(IAdminCategoryGroupService adminCategoryGroupService)
    {
        _adminCategoryGroupService = adminCategoryGroupService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategoryGroups([FromQuery] int? categoryId, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var result = await _adminCategoryGroupService.GetPagedAsync(categoryId, search, page, pageSize);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategoryGroup([FromForm] CategoryGroupCreateDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _adminCategoryGroupService.CreateAsync(dto);
        if (!result.Success || result.Data == null)
        {
            return BadRequest(new { message = result.Error });
        }

        var newGroup = await _adminCategoryGroupService.GetByIdAsync(result.Data.Id);

        return CreatedAtAction(nameof(GetCategoryGroup), new { id = result.Data.Id }, newGroup.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategoryGroup(int id)
    {
        if (id <= 0) return BadRequest("Invalid category group ID.");

        var result = await _adminCategoryGroupService.GetByIdAsync(id);

        if (!result.Success)
        {
            return NotFound(new { message = result.Error });
        }

        return Ok(result.Data);
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategoryGroup(int id, [FromForm] CategoryGroupUpdateDto dto)
    {
        if (id <= 0) return BadRequest("Invalid category group ID.");
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _adminCategoryGroupService.UpdateAsync(id, dto);

        if (result.Success) return NoContent();

        return result.Error switch
        {
            "Category group not found." => NotFound(new { message = result.Error }),
            "This record was modified by another user. Please refresh and try again." => Conflict(new { message = result.Error }),
            _ => BadRequest(new { message = result.Error })
        };
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategoryGroup(int id)
    {
        if (id <= 0) return BadRequest("Invalid category group ID.");

        var result = await _adminCategoryGroupService.DeleteAsync(id);
        if (!result.Success)
        {
            return result.Error switch
            {
                "Category group not found." => NotFound(new { message = result.Error }),
                _ => BadRequest(new { message = result.Error })
            };
        }
        return NoContent();
    }
}