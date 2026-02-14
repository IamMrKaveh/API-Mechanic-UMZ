namespace MainApi.Order.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrderStatusController : BaseApiController
{
    private readonly IMediator _mediator;

    public OrderStatusController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

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
        // نیاز به GetOrderStatusByIdQuery
        return StatusCode(501, "Implement GetOrderStatusByIdQuery");
    }
}