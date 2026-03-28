namespace MainApi.Shipping.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CheckoutShippingController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("available-methods")]
    [Authorize]
    public async Task<IActionResult> GetAvailableShippings()
    {
        var query = new GetAvailableShippingsQuery(CurrentUser.UserId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("available-methods-for-variants")]
    [Authorize]
    public async Task<IActionResult> GetAvailableShippingsForVariants([FromBody] List<int> variantIds)
    {
        var result = await _mediator.Send(new GetAvailableShippingsForVariantsQuery(variantIds));
        return ToActionResult(result);
    }

    [HttpGet("calculate")]
    [Authorize]
    public async Task<IActionResult> CalculateShippingCost([FromQuery] int shippingMethodId)
    {
        var query = new CalculateShippingCostQuery(CurrentUser.UserId, shippingMethodId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}