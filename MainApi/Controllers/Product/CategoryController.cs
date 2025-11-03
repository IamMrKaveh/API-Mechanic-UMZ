namespace MainApi.Controllers.Product;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class CategoryController : BaseApiController
{
    private readonly MechanicContext _context;
    private readonly ILogger<CategoryController> _logger;
    private readonly IStorageService _storageService;

    public CategoryController(
        MechanicContext context,
        ILogger<CategoryController> logger,
        IStorageService storageService)
    {
        _context = context;
        _logger = logger;
        _storageService = storageService;
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

            var totalItems = await query.CountAsync();
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
                Items = Categories.Select(c => new
                {
                    c.Id,
                    c.Name,
                    Icon = ToAbsoluteUrl(c.Icon),
                    c.ProductCount,
                    c.TotalValue,
                    c.InStockProducts
                }),
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
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
                    Icon = pt.Icon,
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
                Icon = ToAbsoluteUrl(category.Icon),
                category.ProductCount,
                category.InStockProducts,
                category.TotalValue,
                category.TotalSellingValue,
                Products = new
                {
                    Items = products.Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.Count,
                        p.SellingPrice,
                        p.PurchasePrice,
                        Icon = ToAbsoluteUrl(p.Icon),
                        p.IsInStock
                    }),
                    TotalItems = totalProductCount,
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

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<TCategory>> CreateCategory([FromForm] CategoryDto categoryDto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        if (string.IsNullOrWhiteSpace(categoryDto.Name)) return BadRequest("نام نوع محصول الزامی است");

        var duplicateExists = await _context.TCategory.AnyAsync(pt => pt.Name != null && pt.Name.ToLower() == categoryDto.Name.ToLower());
        if (duplicateExists) return BadRequest("نوع محصولی با این نام قبلاً وجود دارد");

        string? iconUrl = null;
        if (categoryDto.IconFile != null)
        {
            iconUrl = await _storageService.UploadFileAsync(categoryDto.IconFile, "images/category");
        }

        var sanitizer = new HtmlSanitizer();
        var category = new TCategory
        {
            Name = sanitizer.Sanitize(categoryDto.Name.Trim()),
            Icon = ToRelativeUrl(iconUrl)
        };

        _context.TCategory.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction("GetCategory", new { id = category.Id }, new
        {
            category.Id,
            category.Name,
            Icon = ToAbsoluteUrl(category.Icon)
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCategory(int id, [FromForm] CategoryDto categoryDto)
    {
        if (id <= 0 || id != categoryDto.Id) return BadRequest("شناسه نامعتبر است");
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var existingCategory = await _context.TCategory.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (existingCategory == null) return NotFound("نوع محصول یافت نشد");

        var duplicateExists = await _context.TCategory.AnyAsync(pt => pt.Name != null && pt.Name.ToLower() == categoryDto.Name.ToLower() && pt.Id != id);
        if (duplicateExists) return BadRequest("نوع محصولی با این نام قبلاً وجود دارد");

        var sanitizer = new HtmlSanitizer();
        var categoryToUpdate = new TCategory { Id = id, Name = sanitizer.Sanitize(categoryDto.Name.Trim()), Icon = existingCategory.Icon };

        if (categoryDto.IconFile != null)
        {
            if (!string.IsNullOrEmpty(existingCategory.Icon))
            {
                await _storageService.DeleteFileAsync(existingCategory.Icon);
            }
            categoryToUpdate.Icon = ToRelativeUrl(await _storageService.UploadFileAsync(categoryDto.IconFile, "images/category"));
        }

        _context.Entry(categoryToUpdate).State = EntityState.Modified;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        if (id <= 0) return BadRequest("شناسه نامعتبر است");

        var category = await _context.TCategory.Include(pt => pt.Products).FirstOrDefaultAsync(pt => pt.Id == id);
        if (category == null) return NotFound("نوع محصول یافت نشد");
        if (category.Products?.Any() == true) return BadRequest("امکان حذف نوع محصولی که دارای محصول وابسته است وجود ندارد");

        string? iconPath = category.Icon;

        _context.TCategory.Remove(category);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(iconPath))
        {
            await _storageService.DeleteFileAsync(iconPath);
        }

        return NoContent();
    }

    [NonAction]
    private async Task<string> SaveIconFile(IFormFile iconFile)
    {
        if (iconFile.Length > 2 * 1024 * 1024)
            throw new ArgumentException("File size cannot exceed 2MB.");

        var allowedExtensions = new Dictionary<string, string>
    {
        { "image/jpeg", ".jpg" },
        { "image/png", ".png" },
        { "image/webp", ".webp" },
        { "image/gif", ".gif" }
    };

        if (!allowedExtensions.TryGetValue(iconFile.ContentType.ToLower(), out var extension))
            throw new ArgumentException("Invalid file type. Only JPG, PNG, WebP, GIF are allowed.");

        var fileName = $"{Guid.NewGuid()}{extension}";
        var directoryPath = Path.Combine("wwwroot", "images", "category");
        Directory.CreateDirectory(directoryPath);
        var filePath = Path.Combine(directoryPath, fileName);

        if (filePath.Length > 260) // MAX_PATH
            throw new ArgumentException("Generated file path is too long.");

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await iconFile.CopyToAsync(stream);
        }
        return $"images/category/{fileName}";
    }

    [NonAction]
    private void DeleteIconFile(string iconPath)
    {
        if (string.IsNullOrEmpty(iconPath)) return;

        var fullPath = Path.Combine("wwwroot", iconPath.TrimStart('/'));
        if (System.IO.File.Exists(fullPath))
        {
            try
            {
                System.IO.File.Delete(fullPath);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error deleting old category icon: {Path}", fullPath);
            }
        }
    }
}