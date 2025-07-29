namespace MainApi.Controllers.Product;

[Route("api/[controller]")]
[ApiController]
public class ProductTypesController : ControllerBase
{
    private readonly MechanicContext _context;

    public ProductTypesController(MechanicContext context)
    {
        _context = context;
    }

    // GET: api/ProductTypes
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TProductTypes>>> GetTProductTypes()
    {
        return await _context.TProductTypes.ToListAsync();
    }

    // GET: api/ProductTypes/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TProductTypes>> GetTProductTypes(int id)
    {
        var tProductTypes = await _context.TProductTypes.FindAsync(id);

        if (tProductTypes == null)
        {
            return NotFound();
        }

        return tProductTypes;
    }

    // PUT: api/ProductTypes/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTProductTypes(int id, TProductTypes tProductTypes)
    {
        if (id != tProductTypes.Id)
        {
            return BadRequest();
        }

        _context.Entry(tProductTypes).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TProductTypesExists(id))
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

    // POST: api/ProductTypes
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<TProductTypes>> PostTProductTypes(TProductTypes tProductTypes)
    {
        _context.TProductTypes.Add(tProductTypes);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetTProductTypes", new { id = tProductTypes.Id }, tProductTypes);
    }

    // DELETE: api/ProductTypes/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTProductTypes(int id)
    {
        var tProductTypes = await _context.TProductTypes.FindAsync(id);
        if (tProductTypes == null)
        {
            return NotFound();
        }

        _context.TProductTypes.Remove(tProductTypes);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool TProductTypesExists(int id)
    {
        return _context.TProductTypes.Any(e => e.Id == id);
    }
}
