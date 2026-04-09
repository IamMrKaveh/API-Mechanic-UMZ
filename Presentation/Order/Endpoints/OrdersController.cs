using Application.Order.Features.Commands.CancelOrder;
using Application.Order.Features.Commands.CheckoutFromCart;
using Application.Order.Features.Queries.GetOrderDetails;
using Application.Order.Features.Queries.GetUserOrders;
using Presentation.Order.Requests;

namespace Presentation.Order.Endpoints;

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
        var query = new GetUserOrdersQuery(
            CurrentUser.UserId,
            status,
            page,
            pageSize);
        var result = await _mediator.Send(query);
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderById(Guid id)
    {
        var result = await _mediator.Send(new GetOrderDetailsQuery(id, CurrentUser.UserId));
        return ToActionResult(result);
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> CheckoutFromCart(
        [FromBody] CheckoutFromCartRequest request,
        CancellationToken ct)
    {
        var command = new CheckoutFromCartCommand(
            CurrentUser.UserId,
            request.CartId,
            request.ShippingId,
            request.AddressId,
            request.DiscountCode,
            request.PaymentGateway,
            CurrentUser.IpAddress,
            HttpContext.Request.Headers.UserAgent.ToString(),
            Guid.NewGuid());

        var result = await _mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] CancelOrderRequest request)
    {
        var command = new CancelOrderCommand(
            id,
            CurrentUser.UserId,
            request.Reason);

        var result = await _mediator.Send(command);
        return ToActionResult(result);
    }
}