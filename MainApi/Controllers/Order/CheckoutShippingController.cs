using Application.Common.Interfaces.Order;
using Application.Common.Interfaces.User;

namespace MainApi.Controllers.Order;

[Route("api/[controller]")]
[ApiController]
public class CheckoutShippingController : ControllerBase
{
    private readonly ICheckoutShippingService _checkoutShippingService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CheckoutShippingController> _logger;

    public CheckoutShippingController(
        ICheckoutShippingService checkoutShippingService,
        ICurrentUserService currentUserService,
        ILogger<CheckoutShippingController> logger)
    {
        _checkoutShippingService = checkoutShippingService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet("available-methods")]
    [Authorize]
    public async Task<IActionResult> GetAvailableShippingMethods()
    {
        var userId = _currentUserService.UserId;
        if (userId == null) return Unauthorized();

        var result = await _checkoutShippingService.GetAvailableShippingMethodsForCartAsync(userId.Value);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Data);
    }

    [HttpPost("available-methods-for-variants")]
    [Authorize]
    public async Task<IActionResult> GetAvailableShippingMethodsForVariants([FromBody] List<int> variantIds)
    {
        if (variantIds == null || !variantIds.Any())
        {
            return BadRequest(new { message = "لیست محصولات خالی است." });
        }

        var distinctIds = variantIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        if (!distinctIds.Any())
        {
            return BadRequest(new { message = "شناسه محصولات نامعتبر است." });
        }

        var result = await _checkoutShippingService.GetAvailableShippingMethodsForVariantsAsync(distinctIds);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Error });
        }

        return Ok(result.Data);
    }
}