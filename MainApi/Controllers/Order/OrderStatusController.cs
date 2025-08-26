namespace MainApi.Controllers.Order;

[Route("api/[controller]")]
[ApiController]
[Authorize]
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
                OrderCount = s.Orders != null ? s.Orders.Count() : 0
            })
            .ToListAsync();

        return Ok(orderStatuses);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetTOrderStatus(int id)
    {
        if (id <= 0)
            return BadRequest("Invalid order status ID");

        var orderStatus = await _context.TOrderStatus
            .Include(s => s.Orders)
            .Select(s => new
            {
                s.Id,
                s.Name,
                s.Icon,
                OrderCount = s.Orders != null ? s.Orders.Count() : 0,
                Orders = s.Orders != null ? s.Orders.Select(o => new
                {
                    o.Id,
                    o.Name,
                    o.TotalAmount,
                    o.CreatedAt,
                    User = new
                    {
                        o.User.Id,
                        o.User.PhoneNumber,
                        o.User.FirstName,
                        o.User.LastName
                    }
                }).Cast<object>().ToList() : new List<object>()
            })
            .FirstOrDefaultAsync(s => s.Id == id);

        if (orderStatus == null)
            return NotFound();

        return Ok(orderStatus);
    }

    [HttpPost]
    public async Task<ActionResult<TOrderStatus>> PostTOrderStatus(CreateOrderStatusDto statusDto)
    {
        if (statusDto == null)
            return BadRequest("Order status data is required");

        if (string.IsNullOrWhiteSpace(statusDto.Name))
            return BadRequest("Name is required");

        var existingStatus = await _context.TOrderStatus
            .FirstOrDefaultAsync(s => s.Name.ToLower() == statusDto.Name.ToLower());

        if (existingStatus != null)
            return BadRequest("Order status with this name already exists");

        var orderStatus = new TOrderStatus
        {
            Name = statusDto.Name.Trim(),
            Icon = statusDto.Icon?.Trim()
        };

        _context.TOrderStatus.Add(orderStatus);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetTOrderStatus", new { id = orderStatus.Id }, orderStatus);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutTOrderStatus(int id, UpdateOrderStatusDto statusDto)
    {
        if (id <= 0)
            return BadRequest("Invalid order status ID");

        if (statusDto == null)
            return BadRequest("Order status data is required");

        var orderStatus = await _context.TOrderStatus.FindAsync(id);
        if (orderStatus == null)
            return NotFound();

        if (statusDto.Name != null)
        {
            if (string.IsNullOrWhiteSpace(statusDto.Name))
                return BadRequest("Name cannot be empty");

            var existingStatus = await _context.TOrderStatus
                .FirstOrDefaultAsync(s => s.Name.ToLower() == statusDto.Name.ToLower() && s.Id != id);

            if (existingStatus != null)
                return BadRequest("Order status with this name already exists");

            orderStatus.Name = statusDto.Name.Trim();
        }

        if (statusDto.Icon != null)
            orderStatus.Icon = statusDto.Icon.Trim();

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
        if (id <= 0)
            return BadRequest("Invalid order status ID");

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