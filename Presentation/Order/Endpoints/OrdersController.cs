using Application.Order.Features.Commands.CancelOrder;
using Application.Order.Features.Commands.CheckoutFromCart;
using Application.Order.Features.Queries.GetOrderDetails;
using Application.Order.Features.Queries.GetUserOrders;
using Application.Order.Features.Shared;
using Presentation.Order.Requests;

namespace Presentation.Order.Endpoints;

[Route("api/v{version:apiVersion}/orders")]
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
            RequestContext.UserId ?? Guid.Empty,
            request.Status,
            request.Page,
            request.PageSize);

        return await Send(query, ct);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken ct)
    {
        return await Send(new GetOrderDetailsQuery(id, RequestContext.UserId ?? Guid.Empty), ct);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CheckoutResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckoutFromCart(
        [FromBody] CheckoutFromCartRequest request,
        CancellationToken ct)
    {
        var command = new CheckoutFromCartCommand(
            RequestContext.UserId ?? Guid.Empty,
            request.CartId,
            request.ShippingId,
            request.AddressId,
            request.DiscountCode,
            request.PaymentGateway,
            RequestContext.IpAddress ?? string.Empty,
            RequestContext.UserAgent ?? string.Empty,
            Guid.NewGuid());

        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/cancellation")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequest request,
        CancellationToken ct)
    {
        return await Send(new CancelOrderCommand(id, request.Reason), ct);
    }
}