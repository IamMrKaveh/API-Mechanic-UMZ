using Application.Order.Features.Commands.DeleteOrder;
using Application.Order.Features.Commands.ExpireOrders;
using Application.Order.Features.Commands.MarkOrderAsShipped;
using Application.Order.Features.Commands.UpdateOrderStatus;
using Application.Order.Features.Queries.GetAdminOrderById;
using Application.Order.Features.Queries.GetAdminOrders;
using Application.Order.Features.Queries.GetOrderStatistics;
using Application.Order.Features.Shared;
using Presentation.Order.Requests;

namespace Presentation.Order.Endpoints;

[ApiController]
[Route("api/v{version:apiVersion}/admin/orders")]
[Authorize(Roles = "Admin")]
public class AdminOrdersController(
    IMediator mediator,
    IMapper mapper) : BaseApiController(mediator, mapper)
{
    private const string IfMatchHeader = "If-Match";

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<AdminOrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromQuery] GetAdminOrdersRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetAdminOrdersQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<AdminOrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken ct)
    {
        var query = new GetAdminOrderByIdQuery(id);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<OrderStatisticsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatistics(
        [FromQuery] GetOrderStatisticsRequest request,
        CancellationToken ct)
    {
        var query = Mapper.Map<GetOrderStatisticsQuery>(request);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpPost("expiration")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExpireOrders(CancellationToken ct)
    {
        var command = new ExpireOrdersCommand();
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> UpdateOrderStatus(
        Guid id,
        [FromBody] UpdateOrderStatusByIdRequest request,
        [FromHeader(Name = IfMatchHeader)] string? ifMatch,
        CancellationToken ct)
    {
        var command = new UpdateOrderStatusCommand(id, request.NewStatus, StripQuotes(ifMatch) ?? string.Empty);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrder(Guid id, CancellationToken ct)
    {
        var command = new DeleteOrderCommand(id);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    [HttpPatch("{id:guid}/ship")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status412PreconditionFailed)]
    public async Task<IActionResult> MarkAsShipped(
        Guid id,
        [FromHeader(Name = IfMatchHeader)] string? ifMatch,
        CancellationToken ct)
    {
        var command = new MarkOrderAsShippedCommand(id, StripQuotes(ifMatch));
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }

    private static string? StripQuotes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
            return trimmed[1..^1];
        return trimmed;
    }
}
