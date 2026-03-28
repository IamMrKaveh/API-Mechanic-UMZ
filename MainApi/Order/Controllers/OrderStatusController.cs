namespace MainApi.Order.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrderStatusController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrderStatuses()
    {
        var query = new GetOrderStatusesQuery();
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetOrderStatus(int id)
    {
        var query = new GetOrderStatusByIdQuery(id);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }
}