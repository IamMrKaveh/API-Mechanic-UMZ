using Application.Brand.Features.Queries.GetBrand;
using Application.Brand.Features.Queries.GetPublicBrands;
using MapsterMapper;
using Presentation.Brand.Requests;

namespace Presentation.Brand.Endpoints;

[Route("api/v{version:apiVersion}/brand")]
[ApiController]
public sealed class BrandController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetBrands(
        [FromQuery] GetPublicBrandsRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetPublicBrandsQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBrand(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetBrandQuery(id), ct);
        return ToActionResult(result);
    }
}