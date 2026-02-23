namespace MainApi.Order.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderItemsController : BaseApiController
{
    private readonly IMediator _mediator;

    public OrderItemsController(IMediator mediator, ICurrentUserService currentUserService) : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderItem(int id)
    {
        var result = await _mediator.Send(new GetOrderItemByIdQuery(id));
        return ToActionResult(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrderItem(int id, [FromBody] UpdateOrderItemDto itemDto)
    {
        var result = await _mediator.Send(new UpdateOrderItemCommand(id, itemDto));
        return ToActionResult(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrderItem(int id)
    {
        var result = await _mediator.Send(new DeleteOrderItemCommand(id));
        return ToActionResult(result);
    }
}