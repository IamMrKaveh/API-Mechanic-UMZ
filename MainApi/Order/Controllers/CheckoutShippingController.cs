namespace MainApi.Order.Controllers;

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
    public async Task<IActionResult> GetAvailableShippingMethods()
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();
        var query = new GetAvailableShippingMethodsQuery(CurrentUser.UserId.Value);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("available-methods-for-variants")]
    [Authorize]
    public async Task<IActionResult> GetAvailableShippingMethodsForVariants([FromBody] List<int> variantIds)
    {
        var result = await _mediator.Send(new GetAvailableShippingMethodsForVariantsQuery(variantIds));
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