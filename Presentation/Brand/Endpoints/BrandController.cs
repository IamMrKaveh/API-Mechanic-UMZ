using Application.Brand.Features.Queries.GetBrand;
using Application.Brand.Features.Queries.GetPublicBrands;

namespace Presentation.Brand.Endpoints;

[Route("api/[controller]")]
[ApiController]
public class BrandController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetBrands([FromQuery] Guid? categoryId)
    {
        var query = new GetPublicBrandsQuery(categoryId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBrand(Guid id)
    {
        var query = new GetBrandQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}