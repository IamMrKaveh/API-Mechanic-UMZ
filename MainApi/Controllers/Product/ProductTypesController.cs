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
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<object>>> GetProductTypes([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        try
        {
            var query = _context.TProductTypes.Include(pt => pt.Products).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(pt => pt.Name != null && pt.Name.ToLower().Contains(search.ToLower()));
            }

            var totalCount = await query.CountAsync();
            var productTypes = await query
                .Select(pt => new
                {
                    pt.Id,
                    pt.Name,
                    pt.Icon,
                    ProductCount = pt.Products != null ? pt.Products.Count : 0,
                    TotalValue = pt.Products != null ? pt.Products.Sum(p => (p.Count ?? 0) * (p.PurchasePrice ?? 0)) : 0,
                    InStockProducts = pt.Products != null ? pt.Products.Count(p => (p.Count ?? 0) > 0) : 0
                })
                .OrderBy(pt => pt.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new
            {
                Data = productTypes,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "خطا در دریافت انواع محصولات");
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetProductType(int id)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest("شناسه نامعتبر است");
            }

            var productType = await _context.TProductTypes
                .Include(pt => pt.Products)
                .Where(pt => pt.Id == id)
                .Select(pt => new
                {
                    pt.Id,
                    pt.Name,
                    pt.Icon,
                    Products = pt.Products != null ? pt.Products.Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Count,
                        p.SellingPrice,
                        p.PurchasePrice,
                        p.Icon,
                        IsInStock = (p.Count ?? 0) > 0
                    }).OrderBy(p => p.Name).ToList() : null,
                    ProductCount = pt.Products != null ? pt.Products.Count : 0,
                    InStockProducts = pt.Products != null ? pt.Products.Count(p => (p.Count ?? 0) > 0) : 0,
                    TotalValue = pt.Products != null ? pt.Products.Sum(p => (p.Count ?? 0) * (p.PurchasePrice ?? 0)) : 0,
                    TotalSellingValue = pt.Products != null ? pt.Products.Sum(p => (p.Count ?? 0) * (p.SellingPrice ?? 0)) : 0
                })
                .FirstOrDefaultAsync();

            if (productType == null)
            {
                return NotFound("نوع محصول یافت نشد");
            }

            return Ok(productType);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "خطا در دریافت نوع محصول");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProductType(int id, ProductTypeDto productTypeDto)
    {
        try
        {
            if (id <= 0 || id != productTypeDto.Id)
            {
                return BadRequest("شناسه نامعتبر است");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(productTypeDto.Name))
            {
                return BadRequest("نام نوع محصول الزامی است");
            }

            var existingProductType = await _context.TProductTypes.FindAsync(id);
            if (existingProductType == null)
            {
                return NotFound("نوع محصول یافت نشد");
            }

            var duplicateExists = await _context.TProductTypes
                .AnyAsync(pt => pt.Name != null && pt.Name.ToLower() == productTypeDto.Name.ToLower() && pt.Id != id);
            if (duplicateExists)
            {
                return BadRequest("نوع محصولی با این نام قبلاً وجود دارد");
            }

            existingProductType.Name = productTypeDto.Name.Trim();
            existingProductType.Icon = string.IsNullOrWhiteSpace(productTypeDto.Icon) ? null : productTypeDto.Icon.Trim();

            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await ProductTypeExistsAsync(id))
            {
                return NotFound("نوع محصول یافت نشد");
            }
            return Conflict("تضاد در به‌روزرسانی داده‌ها");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "خطا در به‌روزرسانی نوع محصول");
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TProductTypes>> CreateProductType(ProductTypeDto productTypeDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(productTypeDto.Name))
            {
                return BadRequest("نام نوع محصول الزامی است");
            }

            var duplicateExists = await _context.TProductTypes
                .AnyAsync(pt => pt.Name != null && pt.Name.ToLower() == productTypeDto.Name.ToLower());
            if (duplicateExists)
            {
                return BadRequest("نوع محصولی با این نام قبلاً وجود دارد");
            }

            var productType = new TProductTypes
            {
                Name = productTypeDto.Name.Trim(),
                Icon = string.IsNullOrWhiteSpace(productTypeDto.Icon) ? null : productTypeDto.Icon.Trim()
            };

            _context.TProductTypes.Add(productType);
            await _context.SaveChangesAsync();

            var result = new
            {
                productType.Id,
                productType.Name,
                productType.Icon
            };

            return CreatedAtAction("GetProductType", new { id = productType.Id }, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, "خطا در ایجاد نوع محصول");
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProductType(int id)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest("شناسه نامعتبر است");
            }

            var productType = await _context.TProductTypes
                .Include(pt => pt.Products)
                .FirstOrDefaultAsync(pt => pt.Id == id);

            if (productType == null)
            {
                return NotFound("نوع محصول یافت نشد");
            }

            if (productType.Products?.Any() == true)
            {
                return BadRequest("امکان حذف نوع محصولی که دارای محصول وابسته است وجود ندارد");
            }

            _context.TProductTypes.Remove(productType);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, "خطا در حذف نوع محصول");
        }
    }

    [NonAction]
    private async Task<bool> ProductTypeExistsAsync(int id)
    {
        return await _context.TProductTypes.AnyAsync(e => e.Id == id);
    }
}