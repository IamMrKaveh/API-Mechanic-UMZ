namespace Presentation.Shipping.Endpoints;

[ApiController]
[Route("api/[controller]")]
public class ShippingController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetActiveShippings()
    {
        var query = new GetShippingsQuery(false);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}