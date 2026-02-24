namespace MainApi.Brand.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BrandController : BaseApiController
{
    private readonly IMediator _mediator;

    public BrandController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetBrands(
        [FromQuery] int? categoryId,
        [FromQuery] string? search,
        [FromQuery] bool? IsActive,
        [FromQuery] bool IncludeDeleted,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetAdminBrandsQuery(categoryId, search, IsActive, IncludeDeleted, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBrand(int id)
    {
        
        var query = new GetBrandByIdQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}