namespace MainApi.Shipping.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ShippingController : BaseApiController
{
    private readonly IMediator _mediator;

    public ShippingController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveShippings()
    {
        var query = new GetShippingsQuery(false);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}