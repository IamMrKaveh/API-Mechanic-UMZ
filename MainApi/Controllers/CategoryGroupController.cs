namespace MainApi.Controllers
{
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

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategoryGroup([FromForm] CategoryGroupCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _categoryGroupService.CreateAsync(dto);
            if (!result.Success || result.Data == null)
            {
                return BadRequest(new { message = result.Error });
            }

            return CreatedAtAction(nameof(GetCategoryGroup), new { id = result.Data.Id }, result.Data);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategoryGroup(int id, [FromForm] CategoryGroupUpdateDto dto)
        {
            if (id <= 0) return BadRequest("Invalid category group ID.");
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _categoryGroupService.UpdateAsync(id, dto);

            if (result.Success) return NoContent();

            return result.Error switch
            {
                "Category group not found." => NotFound(new { message = result.Error }),
                "This record was modified by another user. Please refresh and try again." => Conflict(new { message = result.Error }),
                _ => BadRequest(new { message = result.Error })
            };
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategoryGroup(int id)
        {
            if (id <= 0) return BadRequest("Invalid category group ID.");

            var result = await _categoryGroupService.DeleteAsync(id);
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
}