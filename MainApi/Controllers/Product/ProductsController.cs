namespace MainApi.Controllers.Product;

[Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly MechanicContext _context;

    public ProductsController(MechanicContext context)
    {
        _context = context;
    }

    // GET: api/Products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TProducts>>> GetTProducts()
    {
        return await _context.TProducts.ToListAsync();
    }

    // GET: api/Products/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TProducts>> GetTProducts(int id)
    {
        var tProducts = await _context.TProducts.FindAsync(id);

        if (tProducts == null)
        {
            return NotFound();
        }

        return tProducts;
    }

    // PUT: api/Products/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTProducts(int id, TProducts tProducts)
    {
        if (id != tProducts.Id)
        {
            return BadRequest();
        }

        _context.Entry(tProducts).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!TProductsExists(id))
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

    // POST: api/Products
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<TProducts>> PostTProducts(TProducts tProducts)
    {
        _context.TProducts.Add(tProducts);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetTProducts", new { id = tProducts.Id }, tProducts);
    }

    // DELETE: api/Products/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTProducts(int id)
    {
        var tProducts = await _context.TProducts.FindAsync(id);
        if (tProducts == null)
        {
            return NotFound();
        }

        _context.TProducts.Remove(tProducts);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool TProductsExists(int id)
    {
        return _context.TProducts.Any(e => e.Id == id);
    }
}
