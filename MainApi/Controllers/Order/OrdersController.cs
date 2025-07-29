namespace MainApi.Controllers.Order;

[Route("api/[controller]")]
[ApiController]
public class OrdersController : ControllerBase
{
    private readonly MechanicContext _context;

    public OrdersController(MechanicContext context)
    {
        _context = context;
    }

    // GET: api/Orders
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TOrders>>> GetTOrders()
    {
        return await _context.TOrders.ToListAsync();
    }

    // GET: api/Orders/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TOrders>> GetTOrders(int id)
    {
        var tOrders = await _context.TOrders.FindAsync(id);

        if (tOrders == null)
        {
            return NotFound();
        }

        return tOrders;
    }

    // PUT: api/Orders/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTOrders(int id, TOrders tOrders)
    {
        if (id != tOrders.Id)
        {
            return BadRequest();
        }

        _context.Entry(tOrders).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TOrdersExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/Orders
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<TOrders>> PostTOrders(TOrders tOrders)
    {
        _context.TOrders.Add(tOrders);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetTOrders", new { id = tOrders.Id }, tOrders);
    }

    // DELETE: api/Orders/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTOrders(int id)
    {
        var tOrders = await _context.TOrders.FindAsync(id);
        if (tOrders == null)
        {
            return NotFound();
        }

        _context.TOrders.Remove(tOrders);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool TOrdersExists(int id)
    {
        return _context.TOrders.Any(e => e.Id == id);
    }
}
