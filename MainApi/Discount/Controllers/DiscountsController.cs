namespace MainApi.Discount.Controllers;

[ApiController]
[Route("api/discounts")]
[Authorize]
public class DiscountsController : BaseApiController
{
    private readonly IMediator _mediator;

    public DiscountsController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidateDiscountRequest request)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var query = new ValidateDiscountQuery(request.Code, request.OrderTotal, CurrentUser.UserId.Value);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("apply")]
    public async Task<IActionResult> Apply([FromBody] ApplyDiscountRequest request)
    {
        if (!CurrentUser.UserId.HasValue) return Unauthorized();

        var command = new ApplyDiscountCommand(request.Code, request.OrderTotal, CurrentUser.UserId.Value);
        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}