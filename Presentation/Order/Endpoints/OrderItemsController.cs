using Application.Order.Features.Commands.DeleteOrderItem;

namespace Presentation.Order.Endpoints;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderItemsController(IMediator mediator, IMapper mapper) : BaseApiController(mediator, mapper)
{
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrderItem(Guid id, CancellationToken ct)
    {
        var command = new DeleteOrderItemCommand(id);
        var result = await Mediator.Send(command, ct);
        return ToActionResult(result);
    }
}