namespace MainApi.Categories.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoryGroupController : BaseApiController
{
    private readonly IMediator _mediator;

    public CategoryGroupController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryGroups(
        [FromQuery] int? categoryId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetAdminCategoryGroupsLegacyQuery(categoryId, search, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryGroup(int id)
    {
        // برای کاربر عادی ممکن است DTO متفاوتی نیاز باشد، فعلا از همان Detail استفاده می‌کنیم
        var query = new GetCategoryGroupByIdQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}