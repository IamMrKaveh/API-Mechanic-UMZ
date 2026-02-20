namespace MainApi.Category.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CategoryController : BaseApiController
{
    private readonly IMediator _mediator;

    public CategoryController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet("hierarchy")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryHierarchy()
    {
        var query = new GetCategoryTreeQuery();
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    // استفاده از Legacy Query برای لیست ساده
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetAdminCategoriesLegacyQuery(search, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    // محصولات یک دسته‌بندی
    [HttpGet("{id}/products")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoryProducts(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetCategoryProductsQuery(id, true, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}