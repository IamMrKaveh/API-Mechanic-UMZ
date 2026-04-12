using Application.Order.Features.Commands.DeleteOrderItem;
using MapsterMapper;

namespace Presentation.Order.Endpoints;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderItemsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteOrderItem(Guid id, CancellationToken ct)
    {
        var result = await Mediator.Send(new DeleteOrderItemCommand(id), ct);
        return ToActionResult(result);
    }
}