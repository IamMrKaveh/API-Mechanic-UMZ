namespace MainApi.Order.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class OrderItemsController : BaseApiController
{
    private readonly IMediator _mediator;

    public OrderItemsController(IMediator mediator, ICurrentUserService currentUserService)
        : base(currentUserService)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrderItem([FromBody] CreateOrderItemDto itemDto)
    {
        // نیاز به CreateOrderItemCommand
        return StatusCode(501, "Implement CreateOrderItemCommand");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrderItem(int id)
    {
        // نیاز به GetOrderItemByIdQuery
        return StatusCode(501, "Implement GetOrderItemByIdQuery");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateOrderItem(int id, [FromBody] UpdateOrderItemDto itemDto)
    {
        // نیاز به UpdateOrderItemCommand
        return StatusCode(501, "Implement UpdateOrderItemCommand");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrderItem(int id)
    {
        // نیاز به DeleteOrderItemCommand
        return StatusCode(501, "Implement DeleteOrderItemCommand");
    }
}

public class CreateOrderItemDto { }
public class UpdateOrderItemDto { }