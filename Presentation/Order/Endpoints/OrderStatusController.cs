using Application.Order.Features.Queries.GetOrderStatusById;
using Application.Order.Features.Queries.GetOrderStatuses;

namespace Presentation.Order.Endpoints;

[Route("api/order-statuses")]
[ApiController]
[AllowAnonymous]
public class OrderStatusController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetOrderStatuses()
    {
        var result = await _mediator.Send(new GetOrderStatusesQuery());
        return ToActionResult(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderStatusById(Guid id)
    {
        var result = await _mediator.Send(new GetOrderStatusByIdQuery(id));
        return ToActionResult(result);
    }
}