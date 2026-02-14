namespace MainApi.Order.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShippingMethodsController : BaseApiController
{
    private readonly IMediator _mediator;

    public ShippingMethodsController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveShippingMethods()
    {
        var query = new GetShippingMethodsQuery(false); // includeDeleted = false
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}