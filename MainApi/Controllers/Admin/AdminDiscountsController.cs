using Application.Common.Interfaces.Admin.Discount;
using Application.DTOs.Discount;

namespace MainApi.Controllers.Admin;

[ApiController]
[Route("api/admin/discounts")]
[Authorize(Roles = "Admin")]
public class AdminDiscountsController : ControllerBase
{
    private readonly IAdminDiscountService _adminDiscountService;
    private readonly ILogger<AdminDiscountsController> _logger;

    public AdminDiscountsController(
        IAdminDiscountService adminDiscountService,
        ILogger<AdminDiscountsController> logger)
    {
        _adminDiscountService = adminDiscountService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetDiscounts(
        [FromQuery] bool includeExpired = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _adminDiscountService.GetDiscountsAsync(includeExpired, page, pageSize);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDiscount(int id)
    {
        var result = await _adminDiscountService.GetDiscountByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(new { message = result.Error });
        }
        return Ok(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> CreateDiscount([FromBody] CreateDiscountDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _adminDiscountService.CreateDiscountAsync(dto);
        if (!result.Success)
        {
            return Conflict(new { message = result.Error });
        }
        return CreatedAtAction(nameof(GetDiscount), new { id = result.Data!.Id }, result.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateDiscount(int id, [FromBody] UpdateDiscountDto dto)
    {
        var result = await _adminDiscountService.UpdateDiscountAsync(id, dto);
        if (!result.Success)
        {
            if (result.Error?.Contains("modified by another user") == true)
            {
                return Conflict(new { message = result.Error });
            }
            return BadRequest(new { message = result.Error });
        }
        return Ok(new { message = "Discount updated successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteDiscount(int id)
    {
        var result = await _adminDiscountService.DeleteDiscountAsync(id);
        if (!result.Success)
        {
            return NotFound(new { message = result.Error });
        }
        return Ok(new { message = "Discount deleted successfully" });
    }

    [HttpGet("{id}/usages")]
    public async Task<IActionResult> GetDiscountUsages(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _adminDiscountService.GetDiscountUsagesAsync(id, page, pageSize);
        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }
        return Ok(result.Data);
    }
}