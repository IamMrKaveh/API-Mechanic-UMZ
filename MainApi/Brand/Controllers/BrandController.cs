namespace MainApi.Brand.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BrandController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetBrands([FromQuery] int? categoryId)
    {
        var query = new GetPublicBrandsQuery(categoryId);
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