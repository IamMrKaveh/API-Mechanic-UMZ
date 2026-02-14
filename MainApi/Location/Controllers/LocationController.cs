namespace MainApi.Location.Controllers;

[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
public class LocationController : BaseApiController
{
    private readonly IMediator _mediator;

    public LocationController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet("states")]
    public async Task<IActionResult> GetStates()
    {
        var query = new GetStatesQuery();
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("cities")]
    public async Task<IActionResult> GetCities([FromQuery] int stateId)
    {
        var query = new GetCitiesQuery(stateId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}