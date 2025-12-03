using Application.Common.Interfaces.Admin;

namespace MainApi.Controllers.Admin;

[ApiController]
[Route("api/admin/shipping-methods")]
[Authorize(Roles = "Admin")]
public class AdminShippingMethodsController : ControllerBase
{
    private readonly IAdminShippingMethodService _adminShippingMethodService;
    private readonly ICurrentUserService _currentUserService;

    public AdminShippingMethodsController(IAdminShippingMethodService adminShippingMethodService, ICurrentUserService currentUserService)
    {
        _adminShippingMethodService = adminShippingMethodService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<IActionResult> GetShippingMethods([FromQuery] bool includeDeleted = false)
    {
        var result = await _adminShippingMethodService.GetShippingMethodsAsync(includeDeleted);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetShippingMethodById(int id)
    {
        var result = await _adminShippingMethodService.GetShippingMethodByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> CreateShippingMethod([FromBody] ShippingMethodCreateDto dto)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _adminShippingMethodService.CreateShippingMethodAsync(dto, currentUserId.Value);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return CreatedAtAction(nameof(GetShippingMethodById), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateShippingMethod(int id, [FromBody] ShippingMethodUpdateDto dto)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _adminShippingMethodService.UpdateShippingMethodAsync(id, dto, currentUserId.Value);
        if (!result.Success)
        {
            if (result.Error?.Contains("modified by another user") == true)
            {
                return Conflict(new { message = result.Error });
            }
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Shipping method updated successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteShippingMethod(int id)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _adminShippingMethodService.DeleteShippingMethodAsync(id, currentUserId.Value);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Shipping method deleted successfully" });
    }

    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreShippingMethod(int id)
    {
        var currentUserId = _currentUserService.UserId;
        if (!currentUserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _adminShippingMethodService.RestoreShippingMethodAsync(id, currentUserId.Value);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Shipping method restored successfully" });
    }
}