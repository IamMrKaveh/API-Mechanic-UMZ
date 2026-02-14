namespace MainApi.Categories.Controllers;

[Route("api/admin/categories")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminCategoryController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminCategoryController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

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
    public async Task<IActionResult> GetCategory(int id)
    {
        // استفاده از Query موجود که دیتیل را برمی‌گرداند (شامل گروه‌ها)
        var query = new GetCategoryWithGroupsQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategory([FromForm] CreateCategoryCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSucceed)
        {
            return CreatedAtAction(nameof(GetCategory), new { id = result.Data }, result.Data);
        }
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategory(int id, [FromForm] UpdateCategoryCommand command)
    {
        if (id != command.Id) return BadRequest("Mismatched Category ID");

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var command = new DeleteCategoryCommand(id, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderCategories([FromBody] ReorderCategoriesCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}