using Application.Order.Features.Queries.GetOrderStatusById;
using Application.Order.Features.Queries.GetOrderStatuses;
using MapsterMapper;

namespace Presentation.Order.Endpoints;

[Route("api/order-statuses")]
[ApiController]
[AllowAnonymous]
public class OrderStatusController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpGet]
    public async Task<IActionResult> GetOrderStatuses(CancellationToken ct)
    {
        var result = await Mediator.Send(new GetOrderStatusesQuery(), ct);
        return ToActionResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetOrderStatusById(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new GetOrderStatusByIdQuery(id), ct);
        return ToActionResult(result);
    }
}