namespace MainApi.Categories.Controllers;

[Route("api/admin/category-groups")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminCategoryGroupController : BaseApiController
{
    private readonly IMediator _mediator;

    public AdminCategoryGroupController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategoryGroups(
        [FromQuery] int? categoryId,
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetAdminCategoryGroupsQuery(categoryId, search, isActive, includeDeleted, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCategoryGroup([FromForm] CreateCategoryGroupCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSucceed)
        {
            return CreatedAtAction(nameof(GetCategoryGroup), new { id = result.Data }, result.Data);
        }
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCategoryGroup(int id)
    {
        var query = new GetCategoryGroupDetailQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCategoryGroup(int id, [FromForm] UpdateCategoryGroupCommand command)
    {
        if (id != command.GroupId) return BadRequest("Mismatched Group ID"); // Note: Command uses GroupId

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategoryGroup(int id, [FromQuery] int categoryId)
    {
        var command = new DeleteCategoryGroupCommand(categoryId, id, CurrentUser.UserId);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }

    [HttpPost("move")]
    public async Task<IActionResult> MoveCategoryGroup([FromBody] MoveCategoryGroupCommand command)
    {
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}