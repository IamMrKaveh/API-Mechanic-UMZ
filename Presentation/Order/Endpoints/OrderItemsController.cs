using Application.Order.Features.Commands.DeleteOrderItem;

namespace Presentation.Order.Endpoints;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderItemsController(IMediator mediator) : BaseApiController(mediator)
{
    private readonly IMediator _mediator = mediator;

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrderItem(Guid id)
    {
        var result = await _mediator.Send(new DeleteOrderItemCommand(id));
        return ToActionResult(result);
    }
}