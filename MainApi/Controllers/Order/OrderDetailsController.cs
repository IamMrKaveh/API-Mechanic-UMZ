namespace MainApi.Controllers.Order;

[Route("api/[controller]")]
[ApiController]
public class OrderDetailsController : ControllerBase
{
    private readonly MechanicContext _context;

    public OrderDetailsController(MechanicContext context)
    {
        _context = context;
    }

    // GET: api/OrderDetails
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TOrderDetails>>> GetTOrderDetails()
    {
        return await _context.TOrderDetails.ToListAsync();
    }

    // GET: api/OrderDetails/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TOrderDetails>> GetTOrderDetails(int id)
    {
        var tOrderDetails = await _context.TOrderDetails.FindAsync(id);

        if (tOrderDetails == null)
        {
            return NotFound();
        }

        return tOrderDetails;
    }

    // PUT: api/OrderDetails/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTOrderDetails(int id, TOrderDetails tOrderDetails)
    {
        if (id != tOrderDetails.Id)
        {
            return BadRequest();
        }

        _context.Entry(tOrderDetails).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TOrderDetailsExists(id))
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

    // POST: api/OrderDetails
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<TOrderDetails>> PostTOrderDetails(TOrderDetails tOrderDetails)
    {
        _context.TOrderDetails.Add(tOrderDetails);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetTOrderDetails", new { id = tOrderDetails.Id }, tOrderDetails);
    }

    // DELETE: api/OrderDetails/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTOrderDetails(int id)
    {
        var tOrderDetails = await _context.TOrderDetails.FindAsync(id);
        if (tOrderDetails == null)
        {
            return NotFound();
        }

        _context.TOrderDetails.Remove(tOrderDetails);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool TOrderDetailsExists(int id)
    {
        return _context.TOrderDetails.Any(e => e.Id == id);
    }
}
