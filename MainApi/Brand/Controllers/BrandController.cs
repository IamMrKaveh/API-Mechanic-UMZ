namespace MainApi.Brand.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BrandController(IMediator mediator, ICurrentUserService currentUserService) : BaseApiController(currentUserService)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetBrands(
        [FromQuery] int? categoryId,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetPublicBrandsQuery(categoryId, search, page, pageSize);
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