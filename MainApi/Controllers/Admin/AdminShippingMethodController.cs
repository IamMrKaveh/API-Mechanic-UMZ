namespace MainApi.Controllers.Admin;

[Route("api/admin/shipping-methods")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminShippingMethodController : ControllerBase
{
    private readonly IAdminShippingMethodService _shippingMethodService;
    private readonly ICurrentUserService _currentUserService;

    public AdminShippingMethodController(IAdminShippingMethodService shippingMethodService, ICurrentUserService currentUserService)
    {
        _shippingMethodService = shippingMethodService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetShippingMethods([FromQuery] bool includeDeleted = false)
    {
        var result = await _shippingMethodService.GetShippingMethodsAsync(includeDeleted);
        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetShippingMethodById(int id)
    {
        var result = await _shippingMethodService.GetShippingMethodByIdAsync(id);
        if (result.Data == null) return NotFound();
        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> CreateShippingMethod(ShippingMethodCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var result = await _shippingMethodService.CreateShippingMethodAsync(dto, userId.Value);
        if (!result.Success) return BadRequest(new { message = result.Error });
        return CreatedAtAction(nameof(GetShippingMethodById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateShippingMethod(int id, ShippingMethodUpdateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var result = await _shippingMethodService.UpdateShippingMethodAsync(id, dto, userId.Value);
        if (!result.Success)
        {
            return result.Error switch
            {
                "Shipping method not found." => NotFound(new { message = result.Error }),
                "The record was modified by another user. Please refresh and try again." => Conflict(new { message = result.Error }),
                _ => BadRequest(new { message = result.Error })
            };
        }
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteShippingMethod(int id)
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var result = await _shippingMethodService.DeleteShippingMethodAsync(id, userId.Value);
        if (!result.Success) return NotFound(new { message = "Method not found." });
        return NoContent();
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreShippingMethod(int id)
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var result = await _shippingMethodService.RestoreShippingMethodAsync(id, userId.Value);
        if (!result.Success) return NotFound(new { message = "Method not found or not deleted." });
        return NoContent();
    }
}