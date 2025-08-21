namespace MainApi.Controllers.Product;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ProductTypesController : ControllerBase
{
    private readonly MechanicContext _context;

    public ProductTypesController(MechanicContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetTProductTypes([FromQuery] string? search = null)
    {
        var query = _context.TProductTypes.Include(pt => pt.Products).AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(pt => pt.Name.Contains(search));
        }

        var productTypes = await query
            .Select(pt => new
            {
                pt.Id,
                pt.Name,
                pt.Icon,
                ProductCount = pt.Products != null ? pt.Products.Count : 0,
                TotalValue = pt.Products != null ? pt.Products.Sum(p => (p.Count ?? 0) * (p.PurchasePrice ?? 0)) : 0
            })
            .OrderBy(pt => pt.Name)
            .ToListAsync();

        return Ok(productTypes);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<object>> GetTProductTypes(int id)
    {
        var productType = await _context.TProductTypes
            .Include(pt => pt.Products)
            .Where(pt => pt.Id == id)
            .Select(pt => new
            {
                pt.Id,
                pt.Name,
                pt.Icon,
                Products = pt.Products.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Count,
                    p.SellingPrice
                }).ToList(),
                ProductCount = pt.Products.Count,
                TotalValue = pt.Products.Sum(p => (p.Count ?? 0) * (p.PurchasePrice ?? 0))
            })
            .FirstOrDefaultAsync();

        if (productType == null)
        {
            return NotFound();
        }

        return Ok(productType);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutTProductTypes(int id, ProductTypeDto productTypeDto)
    {
        if (id != productTypeDto.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existingProductType = await _context.TProductTypes.FindAsync(id);
        if (existingProductType == null)
        {
            return NotFound();
        }

        var duplicateExists = await _context.TProductTypes
            .AnyAsync(pt => pt.Name == productTypeDto.Name && pt.Id != id);

        if (duplicateExists)
        {
            return BadRequest("Product type with this name already exists");
        }

        existingProductType.Name = productTypeDto.Name;
        existingProductType.Icon = productTypeDto.Icon;

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

    [HttpPost]
    public async Task<ActionResult<TProductTypes>> PostTProductTypes(ProductTypeDto productTypeDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var duplicateExists = await _context.TProductTypes
            .AnyAsync(pt => pt.Name == productTypeDto.Name);

        if (duplicateExists)
        {
            return BadRequest("Product type with this name already exists");
        }

        var productType = new TProductTypes
        {
            Name = productTypeDto.Name,
            Icon = productTypeDto.Icon
        };

        _context.TProductTypes.Add(productType);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetTProductTypes", new { id = productType.Id }, productType);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTProductTypes(int id)
    {
        var productType = await _context.TProductTypes
            .Include(pt => pt.Products)
            .FirstOrDefaultAsync(pt => pt.Id == id);

        if (productType == null)
        {
            return NotFound();
        }

        if (productType.Products?.Any() == true)
        {
            return BadRequest("Cannot delete product type that has associated products");
        }

        _context.TProductTypes.Remove(productType);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool TProductTypesExists(int id)
    {
        return _context.TProductTypes.Any(e => e.Id == id);
    }
}