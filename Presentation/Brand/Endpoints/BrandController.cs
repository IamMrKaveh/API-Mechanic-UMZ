using Application.Brand.Features.Queries.GetBrand;
using Application.Brand.Features.Queries.GetPublicBrands;
using Application.Brand.Features.Shared;
using Presentation.Brand.Requests;

namespace Presentation.Brand.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/brands")]
public sealed class BrandController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BrandListItemDto>>), StatusCodes.Status200OK)]
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
    [ProducesResponseType(typeof(ApiResponse<BrandDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBrand(Guid id, CancellationToken ct)
    {
        var query = new GetBrandQuery(id);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }
}