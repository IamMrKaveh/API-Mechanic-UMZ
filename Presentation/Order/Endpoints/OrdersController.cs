using Application.Order.Features.Commands.CancelOrder;
using Application.Order.Features.Commands.CheckoutFromCart;
using Application.Order.Features.Commands.ConfirmDelivery;
using Application.Order.Features.Commands.RequestReturn;
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
    private const string IfMatchHeader = "If-Match";

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<OrderListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] GetUserOrdersRequest request,
        CancellationToken ct)
    {
        var query = new GetUserOrdersQuery(
            request.Status,
            request.Page,
            request.PageSize);

        return await Send(query, ct);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken ct) => await Send(new GetOrderDetailsQuery(id), ct);

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CheckoutResultDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckoutFromCart(
        [FromBody] CheckoutFromCartRequest request,
        CancellationToken ct)
    {
        var command = new CheckoutFromCartCommand(
            request.CartId,
            request.ShippingId,
            request.AddressId,
            request.DiscountCode,
            request.PaymentGateway,
            request.PaymentMethodId,
            Guid.NewGuid());

        var result = await Mediator.Send(command, ct);
        return ToCreatedActionResult(result);
    }

    [HttpPatch("{id:guid}/cancellation")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequest request,
        [FromHeader(Name = IfMatchHeader)] string? ifMatch,
        CancellationToken ct)
    {
        return await Send(new CancelOrderCommand(id, request.Reason, StripQuotes(ifMatch)), ct);
    }

    [HttpPatch("{id:guid}/delivery-confirmation")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmDelivery(
        Guid id,
        [FromHeader(Name = IfMatchHeader)] string? ifMatch,
        CancellationToken ct) => await Send(new ConfirmDeliveryCommand(id, StripQuotes(ifMatch)), ct);

    [HttpPatch("{id:guid}/return-request")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RequestReturn(
        Guid id,
        [FromBody] RequestReturnRequest request,
        [FromHeader(Name = IfMatchHeader)] string? ifMatch,
        CancellationToken ct) => await Send(new RequestReturnCommand(id, request.Reason, StripQuotes(ifMatch)), ct);

    private static string? StripQuotes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
            return trimmed[1..^1];
        return trimmed;
    }
}
