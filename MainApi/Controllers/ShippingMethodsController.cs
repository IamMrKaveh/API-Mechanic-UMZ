namespace MainApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShippingMethodsController(IShippingService shippingService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get()
    {
        var result = await shippingService.GetActiveShippingMethodsAsync();
        return Ok(result);
    }
}