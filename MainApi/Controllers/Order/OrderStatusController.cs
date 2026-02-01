using Application.Common.Interfaces.Order;

namespace MainApi.Controllers.Order;

[Route("api/[controller]")]
[ApiController]
public class OrderStatusController : ControllerBase
{
    private readonly IOrderStatusService _orderStatusService;

    public OrderStatusController(IOrderStatusService orderStatusService)
    {
        _orderStatusService = orderStatusService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Domain.Order.OrderStatus>>> GetOrderStatuses()
    {
        var statuses = await _orderStatusService.GetOrderStatusesAsync();
        return Ok(statuses);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<Domain.Order.OrderStatus>> GetOrderStatus(int id)
    {
        if (id <= 0) return BadRequest("Invalid order status ID");
        var status = await _orderStatusService.GetOrderStatusByIdAsync(id);
        if (status == null) return NotFound();
        return Ok(status);
    }
}