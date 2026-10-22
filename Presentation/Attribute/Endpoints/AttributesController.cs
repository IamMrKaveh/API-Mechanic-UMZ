using Application.Attribute.Features.Queries.GetAllAttributeTypes;
using Application.Attribute.Features.Queries.GetAttributeTypeById;
using Application.Attribute.Features.Shared;
using SharedKernel.Models;

namespace Presentation.Attribute.Endpoints;

/// <summary>
/// The public read surface of the attribute catalogue. A storefront needs the
/// attribute types and their values to render the filters of a product, and
/// until now they were only reachable through the admin API.
/// </summary>
[Route("api/v{version:apiVersion}/attributes")]
[ApiController]
[AllowAnonymous]
public class AttributesController(IMediator mediator) : BaseApiController(mediator)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<AttributeTypeDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttributeTypes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new GetAllAttributeTypesQuery(page, pageSize);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AttributeTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAttributeType(Guid id, CancellationToken ct)
    {
        var query = new GetAttributeTypeByIdQuery(id);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }
}
