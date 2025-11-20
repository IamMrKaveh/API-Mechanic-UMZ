namespace MainApi.Controllers.Admin;

[Route("api/admin/shipping-methods")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminShippingMethodController : ControllerBase
{
    private readonly IShippingService _shippingService;

    public AdminShippingMethodController(IShippingService shippingService)
    {
        _shippingService = shippingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetShippingMethods([FromQuery] bool includeDeleted = false)
    {
        var result = await _shippingService.GetShippingMethodsAsync(includeDeleted);
        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetShippingMethodById(int id)
    {
        var result = await _shippingService.GetShippingMethodByIdAsync(id);
        if (result.Data == null) return NotFound();
        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> CreateShippingMethod(ShippingMethodCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _shippingService.CreateShippingMethodAsync(dto);
        if (!result.Success) return BadRequest(new { message = result.Error });
        return CreatedAtAction(nameof(GetShippingMethodById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateShippingMethod(int id, ShippingMethodUpdateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var result = await _shippingService.UpdateShippingMethodAsync(id, dto);
        if (!result.Success)
        {
            return result.Error switch
            {
                "NotFound" => NotFound(new { message = "Method not found." }),
                "Concurrency" => Conflict(new { message = "The record was modified by another user. Please reload and try again." }),
                _ => BadRequest(new { message = result.Error })
            };
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteShippingMethod(int id)
    {
        var result = await _shippingService.DeleteShippingMethodAsync(id);
        if (!result.Success) return NotFound(new { message = "Method not found." });
        return NoContent();
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreShippingMethod(int id)
    {
        var result = await _shippingService.RestoreShippingMethodAsync(id);
        if (!result.Success) return NotFound(new { message = "Method not found or not deleted." });
        return NoContent();
    }
}