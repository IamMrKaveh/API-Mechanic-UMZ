namespace MainApi.Controllers.Order;

[Route("api/[controller]")]
[ApiController]
public class OrderStatusController : ControllerBase
{
    private readonly MechanicContext _context;

    public OrderStatusController(MechanicContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetTOrderStatus()
    {
        var orderStatuses = await _context.TOrderStatus
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Icon,
                OrderCount = s.Orders.Count()
            })
            .ToListAsync();

        return Ok(orderStatuses);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetTOrderStatus(int id)
    {
        var orderStatus = await _context.TOrderStatus
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Icon,
                OrderCount = s.Orders.Count(),
                Orders = s.Orders.Select(o => new
                {
                    o.Id,
                    o.Name,
                    o.TotalAmount,
                    o.CreatedAt
                })
            })
            .FirstOrDefaultAsync(s => s.Id == id);

        if (orderStatus == null)
            return NotFound();

        return Ok(orderStatus);
    }

    [HttpPost]
    public async Task<ActionResult<TOrderStatus>> PostTOrderStatus(CreateOrderStatusDto statusDto)
    {
        var orderStatus = new TOrderStatus
        {
            Name = statusDto.Name,
            Icon = statusDto.Icon
        };

        _context.TOrderStatus.Add(orderStatus);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetTOrderStatus", new { id = orderStatus.Id }, orderStatus);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutTOrderStatus(int id, UpdateOrderStatusDto statusDto)
    {
        var orderStatus = await _context.TOrderStatus.FindAsync(id);
        if (orderStatus == null)
            return NotFound();

        orderStatus.Name = statusDto.Name ?? orderStatus.Name;
        orderStatus.Icon = statusDto.Icon ?? orderStatus.Icon;

        try
        {
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TOrderStatusExists(id))
                return NotFound();
            throw;
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTOrderStatus(int id)
    {
        var orderStatus = await _context.TOrderStatus.FindAsync(id);
        if (orderStatus == null)
            return NotFound();

        var hasOrders = await _context.TOrders.AnyAsync(o => o.OrderStatusId == id);
        if (hasOrders)
            return BadRequest("Cannot delete order status that is being used by orders");

        _context.TOrderStatus.Remove(orderStatus);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool TOrderStatusExists(int id)
    {
        return _context.TOrderStatus.Any(e => e.Id == id);
    }
}