using Application.Common.Interfaces.Order;

namespace MainApi.Controllers.Order;

[ApiController]
[Route("api/[controller]")]
public class ShippingMethodsController : ControllerBase
{
    private readonly IShippingMethodService _shippingService;

    public ShippingMethodsController(IShippingMethodService shippingService)
    {
        _shippingService = shippingService;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveShippingMethods()
    {
        var result = await _shippingService.GetActiveShippingMethodsAsync();
        if (result.Success)
        {
            return Ok(result.Data);
        }
        return BadRequest(new { Message = result.Error });
    }
}