using Application.Order.Features.Commands.DeleteOrderItem;
using Application.Order.Features.Queries.GetOrderItemById;
using Presentation.Base.Controllers.v1;

namespace Presentation.Order.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderItemsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderItem(int id)
    {
        var result = await _mediator.Send(new GetOrderItemByIdQuery(id));
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrderItem(int id)
    {
        var result = await _mediator.Send(new DeleteOrderItemCommand(id));
        return ToActionResult(result);
    }
}