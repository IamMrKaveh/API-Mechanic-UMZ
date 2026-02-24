namespace MainApi.Shipping.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CheckoutShippingController : BaseApiController
{
    private readonly IMediator _mediator;

    public CheckoutShippingController(IMediator mediator, ICurrentUserService currentUserService) : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet("available-methods")]
    [Authorize]
    public async Task<IActionResult> GetAvailableShippings()
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();
        var query = new GetAvailableShippingsQuery(CurrentUser.UserId.Value);
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
        if (!CurrentUser.UserId.HasValue) return Unauthorized();
        var query = new CalculateShippingCostQuery(CurrentUser.UserId.Value, shippingMethodId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}