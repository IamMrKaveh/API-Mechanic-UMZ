using Application.Order.Features.Commands.CancelOrder;
using Application.Order.Features.Commands.CheckoutFromCart;
using Application.Order.Features.Queries.GetOrderDetails;
using Application.Order.Features.Queries.GetUserOrders;
using Application.Order.Features.Shared;
using Presentation.Order.Requests;

namespace Presentation.Order.Endpoints;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrdersController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] GetUserOrdersRequest request,
        CancellationToken ct)
    {
        var query = new GetUserOrdersQuery(
            CurrentUser.UserId,
            request.Status,
            request.Page,
            request.PageSize);

        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken ct)
    {
        var query = new GetOrderDetailsQuery(id, CurrentUser.UserId);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost("checkout")]
    [ProducesResponseType(typeof(ApiResponse<CheckoutResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
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

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequest request,
        CancellationToken ct)
    {
        var command = new CancelOrderCommand(
            id,
            CurrentUser.UserId,
            request.Reason);

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}