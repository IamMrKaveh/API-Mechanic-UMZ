using Application.Location.Features.Queries.GetCities;
using Application.Location.Features.Queries.GetStates;
using Application.Location.Features.Shared;

namespace Presentation.Location.Endpoints;

[Route("api/v{version:apiVersion}/locations")]
[ApiController]
[AllowAnonymous]
public class LocationController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("states")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProvinceDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStates()
    {
        var query = new GetStatesQuery();
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("cities")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CityDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCities([FromQuery] int stateId)
    {
        var query = new GetCitiesQuery(stateId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}