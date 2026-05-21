using Application.Order.Features.Queries.GetOrderStatus;
using Application.Order.Features.Queries.GetOrderStatuses;
using Application.Order.Features.Shared;

namespace Presentation.Order.Endpoints;

[Route("api/v{version:apiVersion}/order-statuses")]
[ApiController]
[AllowAnonymous]
public class OrderStatusController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<OrderStatusDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrderStatuses(CancellationToken ct)
    {
        var query = new GetOrderStatusesQuery();
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderStatusById(Guid id, CancellationToken ct)
    {
        var query = new GetOrderStatusQuery(id, CurrentUser.UserId);
        var result = await Mediator.Send(query, ct);
        return ToActionResult(result);
    }
}