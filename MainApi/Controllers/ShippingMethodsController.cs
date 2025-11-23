namespace MainApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShippingMethodsController : ControllerBase
{
    private readonly IShippingService _shippingService;

    public ShippingMethodsController(IShippingService shippingService)
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