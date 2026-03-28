namespace MainApi.Order.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrdersController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetOrders(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetUserOrdersQuery(CurrentUser.UserId, status, page, pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var query = new GetOrderDetailsQuery(id, CurrentUser.UserId);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> CheckoutFromCart(
        [FromBody] CheckoutFromCartRequest request,
        CancellationToken ct)
    {
        var command = new CheckoutFromCartCommand(request.ShippingAddress, request.PaymentMethod);
        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("{orderId:int}/cancel")]
    public async Task<IActionResult> CancelOrder(
        int orderId,
        CancellationToken ct)
    {
        var command = new CancelOrderCommand(orderId);
        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("{orderId:int}/confirm-delivery")]
    public async Task<IActionResult> ConfirmDelivery(
        int orderId,
        CancellationToken ct)
    {
        var command = new ConfirmDeliveryCommand(orderId);
        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("{orderId:int}/return")]
    public async Task<IActionResult> RequestReturn(
        int orderId,
        [FromBody] RequestReturnRequest request,
        CancellationToken ct)
    {
        var command = new RequestReturnCommand(orderId, request.Reason);
        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }
}