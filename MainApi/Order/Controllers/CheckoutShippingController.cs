namespace MainApi.Order.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CheckoutShippingController : BaseApiController
{
    private readonly IMediator _mediator;

    public CheckoutShippingController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
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
        // این نیاز به یک Query جدید دارد: GetAvailableShippingMethodsForVariantsQuery
        return StatusCode(501, "Implement GetAvailableShippingMethodsForVariantsQuery");
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