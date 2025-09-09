using DataAccessLayer.Models.Product;

namespace MainApi.Controllers.Product;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CategoryController : ControllerBase
{
    private readonly MechanicContext _context;

    public CategoryController(MechanicContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<object>>> GetCategorys([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        try
        {
            var query = _context.TCategory.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(pt => pt.Name != null && pt.Name.ToLower().Contains(search.ToLower()));
            }

            var totalCount = await query.CountAsync();
            var Categories = await query
                .Select(pt => new
                {
                    pt.Id,
                    pt.Name,
                    pt.Icon,
                    pt.ProductCount,
                    pt.TotalValue,
                    pt.InStockProducts
                })
                .OrderBy(pt => pt.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new
            {
                Data = Categories,
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
    public async Task<ActionResult<object>> GetCategory(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest("شناسه نامعتبر است");
            }
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var category = await _context.TCategory
                .Where(pt => pt.Id == id)
                .Select(pt => new
                {
                    pt.Id,
                    pt.Name,
                    pt.Icon,
                    ProductCount = pt.Products != null ? pt.Products.Count() : 0,
                    InStockProducts = pt.Products != null ? pt.Products.Count(p => p.Count > 0) : 0,
                    TotalValue = pt.Products != null ? pt.Products.Sum(p => p.Count * p.PurchasePrice) : 0,
                    TotalSellingValue = pt.Products != null ? pt.Products.Sum(p => p.Count * p.SellingPrice) : 0
                })
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound("نوع محصول یافت نشد");
            }

            var productsQuery = _context.TProducts.Where(p => p.CategoryId == id);

            var totalProductCount = await productsQuery.CountAsync();
            var products = await productsQuery
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Count,
                    p.SellingPrice,
                    p.PurchasePrice,
                    p.Icon,
                    IsInStock = p.Count > 0
                })
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new
            {
                category.Id,
                category.Name,
                category.Icon,
                category.ProductCount,
                category.InStockProducts,
                category.TotalValue,
                category.TotalSellingValue,
                Products = new
                {
                    Data = products,
                    TotalCount = totalProductCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalProductCount / pageSize)
                }
            };

            return Ok(response);
        }
        catch (Exception)
        {
            return StatusCode(500, "خطا در دریافت نوع محصول");
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(int id, [FromForm] CategoryDto categoryDto)
    {
        if (id <= 0 || id != categoryDto.Id)
            return BadRequest("شناسه نامعتبر است");
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var existingCategory = await _context.TCategory.FindAsync(id);
        if (existingCategory == null)
            return NotFound("نوع محصول یافت نشد");

        var duplicateExists = await _context.TCategory
            .AnyAsync(pt => pt.Name != null && pt.Name.ToLower() == categoryDto.Name.ToLower() && pt.Id != id);
        if (duplicateExists)
            return BadRequest("نوع محصولی با این نام قبلاً وجود دارد");

        if (categoryDto.IconFile != null)
        {
            if (categoryDto.IconFile.Length > 2 * 1024 * 1024)
                return BadRequest("File size cannot exceed 2MB.");

            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowedMimeTypes.Contains(categoryDto.IconFile.ContentType.ToLower()))
                return BadRequest("Invalid file type. Only JPG, PNG, WebP, GIF are allowed.");

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(categoryDto.IconFile.FileName)}";
            var filePath = Path.Combine("wwwroot", "uploads", "categories", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await categoryDto.IconFile.CopyToAsync(stream);
            }
            existingCategory.Icon = $"/uploads/categories/{fileName}";
        }

        existingCategory.Name = categoryDto.Name.Trim();

        try
        {
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict("تضاد در به‌روزرسانی داده‌ها");
        }
    }


    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TCategory>> CreateCategory([FromForm] CategoryDto categoryDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (string.IsNullOrWhiteSpace(categoryDto.Name))
            return BadRequest("نام نوع محصول الزامی است");

        var duplicateExists = await _context.TCategory
            .AnyAsync(pt => pt.Name != null && pt.Name.ToLower() == categoryDto.Name.ToLower());
        if (duplicateExists)
            return BadRequest("نوع محصولی با این نام قبلاً وجود دارد");

        string? iconUrl = null;
        if (categoryDto.IconFile != null)
        {
            if (categoryDto.IconFile.Length > 2 * 1024 * 1024)
                return BadRequest("File size cannot exceed 2MB.");

            var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowedMimeTypes.Contains(categoryDto.IconFile.ContentType.ToLower()))
                return BadRequest("Invalid file type. Only JPG, PNG, WebP, GIF are allowed.");

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(categoryDto.IconFile.FileName)}";
            var filePath = Path.Combine("wwwroot", "uploads", "categories", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await categoryDto.IconFile.CopyToAsync(stream);
            }
            iconUrl = $"/uploads/categories/{fileName}";
        }

        var category = new TCategory
        {
            Name = categoryDto.Name.Trim(),
            Icon = iconUrl
        };

        _context.TCategory.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetCategory", new { id = category.Id }, category);
    }


    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest("شناسه نامعتبر است");
            }

            var Category = await _context.TCategory
                .Include(pt => pt.Products)
                .FirstOrDefaultAsync(pt => pt.Id == id);

            if (Category == null)
            {
                return NotFound("نوع محصول یافت نشد");
            }

            if (Category.Products?.Any() == true)
            {
                return BadRequest("امکان حذف نوع محصولی که دارای محصول وابسته است وجود ندارد");
            }

            _context.TCategory.Remove(Category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, "خطا در حذف نوع محصول");
        }
    }

    [NonAction]
    private async Task<bool> CategoryExistsAsync(int id)
    {
        return await _context.TCategory.AnyAsync(e => e.Id == id);
    }
}