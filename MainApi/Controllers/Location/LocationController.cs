using MainApi.Services.Location;

namespace MainApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
public class LocationController : BaseApiController
{
    private readonly ILocationService _locationService;
    private readonly ILogger<LocationController> _logger;

    public LocationController(ILocationService locationService, ILogger<LocationController> logger)
    {
        _locationService = locationService;
        _logger = logger;
    }

    [HttpGet("states")]
    public async Task<IActionResult> GetStates()
    {
        try
        {
            var states = await _locationService.GetStatesAsync();
            return Ok(states);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get states.");
            return StatusCode(500, "An error occurred while fetching states.");
        }
    }

    [HttpGet("cities")]
    public async Task<IActionResult> GetCities([FromQuery] int state_id)
    {
        if (state_id <= 0)
        {
            return BadRequest("A valid state_id is required.");
        }
        try
        {
            var cities = await _locationService.GetCitiesByStateAsync(state_id);
            return Ok(cities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cities for state {StateId}.", state_id);
            return StatusCode(500, "An error occurred while fetching cities.");
        }
    }
}