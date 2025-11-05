namespace MainApi.Services.Category;

public class CategoryService : ICategoryService
{
    private readonly MechanicContext _context;
    private readonly IStorageService _storageService;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly string _baseUrl;

    public CategoryService(
        MechanicContext context,
        IStorageService storageService,
        IHtmlSanitizer htmlSanitizer,
        IConfiguration configuration)
    {
        _context = context;
        _storageService = storageService;
        _htmlSanitizer = htmlSanitizer;
        _baseUrl = configuration["LiaraStorage:BaseUrl"] ?? "https://storage.c2.liara.space/mechanic-umz";
    }

    private string? ToAbsoluteUrl(string? relativeUrl)
    {
        if (string.IsNullOrEmpty(relativeUrl))
            return null;
        if (Uri.IsWellFormedUriString(relativeUrl, UriKind.Absolute))
            return relativeUrl;

        var cleanRelative = relativeUrl.TrimStart('~', '/', 'c');
        return $"{_baseUrl}/{cleanRelative}";
    }

    public async Task<(IEnumerable<object> Categories, int TotalItems)> GetCategoriesAsync(string? search, int page, int pageSize)
    {
        var query = _context.TCategory.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(pt => pt.Name != null && pt.Name.ToLower().Contains(search.ToLower()));
        }

        var totalItems = await query.CountAsync();
        var categories = await query
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

        var result = categories.Select(c => new
        {
            c.Id,
            c.Name,
            Icon = ToAbsoluteUrl(c.Icon),
            c.ProductCount,
            c.TotalValue,
            c.InStockProducts
        });

        return (result, totalItems);
    }

    public async Task<object?> GetCategoryByIdAsync(int id, int page, int pageSize)
    {
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
            return null;
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

        return new
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
    }

    public async Task<object> CreateCategoryAsync(CategoryDto categoryDto)
    {
        if (string.IsNullOrWhiteSpace(categoryDto.Name))
            throw new ArgumentException("Category name is required.");

        var duplicateExists = await _context.TCategory.AnyAsync(pt =>
            pt.Name != null && pt.Name.ToLower() == categoryDto.Name.ToLower());
        if (duplicateExists)
            throw new InvalidOperationException("A category with this name already exists.");

        var category = new TCategory
        {
            Name = _htmlSanitizer.Sanitize(categoryDto.Name.Trim())
        };

        _context.TCategory.Add(category);
        await _context.SaveChangesAsync();

        if (categoryDto.IconFile != null)
        {
            category.Icon = await _storageService.UploadFileAsync(
                categoryDto.IconFile,
                "images/category",
                category.Id
            );
            await _context.SaveChangesAsync();
        }

        return new
        {
            category.Id,
            category.Name,
            Icon = ToAbsoluteUrl(category.Icon)
        };
    }

    public async Task<(bool Success, string? ErrorMessage)> UpdateCategoryAsync(int id, CategoryDto categoryDto)
    {
        var existingCategory = await _context.TCategory.FindAsync(id);
        if (existingCategory == null)
            return (false, "Category not found.");

        var duplicateExists = await _context.TCategory.AnyAsync(pt =>
            pt.Name != null &&
            pt.Name.ToLower() == categoryDto.Name.ToLower() &&
            pt.Id != id);
        if (duplicateExists)
            return (false, "A category with this name already exists.");

        existingCategory.Name = _htmlSanitizer.Sanitize(categoryDto.Name.Trim());

        if (categoryDto.IconFile != null)
        {
            if (!string.IsNullOrEmpty(existingCategory.Icon))
            {
                await _storageService.DeleteFileAsync(existingCategory.Icon);
            }
            existingCategory.Icon = await _storageService.UploadFileAsync(
                categoryDto.IconFile,
                "images/category",
                id
            );
        }

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteCategoryAsync(int id)
    {
        var category = await _context.TCategory.Include(pt => pt.Products).FirstOrDefaultAsync(pt => pt.Id == id);
        if (category == null)
            return (false, "Category not found.");

        if (category.Products?.Any() == true)
            return (false, "Cannot delete a category that has associated products.");

        string? iconPath = category.Icon;

        _context.TCategory.Remove(category);
        await _context.SaveChangesAsync();

        if (!string.IsNullOrEmpty(iconPath))
        {
            await _storageService.DeleteFileAsync(iconPath);
        }

        return (true, null);
    }
}