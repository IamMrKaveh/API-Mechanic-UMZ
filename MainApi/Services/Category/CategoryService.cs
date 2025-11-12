using MainApi.Services.Media;

namespace MainApi.Services.Category;

public class CategoryService : ICategoryService
{
    private readonly MechanicContext _context;
    private readonly IStorageService _storageService;
    private readonly IMediaService _mediaService;
    private readonly IHtmlSanitizer _htmlSanitizer;

    public CategoryService(
        MechanicContext context,
        IStorageService storageService,
        IHtmlSanitizer htmlSanitizer,
        IMediaService mediaService)
    {
        _context = context;
        _storageService = storageService;
        _htmlSanitizer = htmlSanitizer;
        _mediaService = mediaService;
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
            .Include(c => c.CategoryGroups)
                .ThenInclude(cg => cg.Products)
                    .ThenInclude(p => p.Variants)
            .Select(c => new
            {
                c.Id,
                c.Name,
                CategoryGroups = c.CategoryGroups.Select(cg => new
                {
                    cg.Id,
                    cg.Name,
                    ProductCount = cg.Products.Count(),
                    InStockProducts = cg.Products.Count(p => p.Variants.Any(v => v.IsUnlimited || v.Stock > 0)),
                    TotalValue = cg.Products.SelectMany(p => p.Variants).Sum(v => v.PurchasePrice * v.Stock),
                    TotalSellingValue = cg.Products.SelectMany(p => p.Variants).Sum(v => v.SellingPrice * v.Stock),
                }).ToList()
            })
            .OrderBy(pt => pt.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = categories.Select(c => new
        {
            c.Id,
            c.Name,
            Icon = _mediaService.GetPrimaryImageUrlAsync("Category", c.Id).Result,
            c.CategoryGroups
        });

        return (result, totalItems);
    }

    public async Task<object?> GetCategoryByIdAsync(int id, int page, int pageSize)
    {
        var category = await _context.TCategory
            .Include(pt => pt.CategoryGroups)
                .ThenInclude(cg => cg.Products)
                    .ThenInclude(p => p.Variants)
            .Where(pt => pt.Id == id)
            .Select(pt => new
            {
                pt.Id,
                pt.Name,
                CategoryGroups = pt.CategoryGroups.Select(cg => new
                {
                    cg.Id,
                    cg.Name,
                    ProductCount = cg.Products.Count(),
                    InStockProducts = cg.Products.Count(p => p.Variants.Any(v => v.IsUnlimited || v.Stock > 0)),
                    TotalValue = cg.Products.SelectMany(p => p.Variants).Sum(v => v.PurchasePrice * v.Stock),
                    TotalSellingValue = cg.Products.SelectMany(p => p.Variants).Sum(v => v.SellingPrice * v.Stock),
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (category == null)
        {
            return null;
        }

        var productsQuery = _context.TProducts.Where(p => p.CategoryGroup.CategoryId == id);
        var totalProductCount = await productsQuery.CountAsync();
        var products = await productsQuery
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Select(p => new
            {
                p.Id,
                p.Name,
                TotalStock = p.Variants.Sum(v => v.Stock),
                SellingPrice = p.Variants.Any() ? p.Variants.Min(v => v.SellingPrice) : 0,
                PurchasePrice = p.Variants.Any() ? p.Variants.Min(v => v.PurchasePrice) : 0,
                IsInStock = p.Variants.Any(v => v.IsUnlimited || v.Stock > 0)
            })
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new
        {
            category.Id,
            category.Name,
            Icon = await _mediaService.GetPrimaryImageUrlAsync("Category", category.Id),
            category.CategoryGroups,
            Products = new
            {
                Items = products.Select(p => new
                {
                    p.Id,
                    p.Name,
                    Count = p.TotalStock,
                    p.SellingPrice,
                    p.PurchasePrice,
                    Icon = _mediaService.GetPrimaryImageUrlAsync("Product", p.Id).Result,
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
            await _mediaService.AttachFileToEntityAsync(categoryDto.IconFile, "Category", category.Id, true);
        }

        return new
        {
            category.Id,
            category.Name,
            Icon = await _mediaService.GetPrimaryImageUrlAsync("Category", category.Id)
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
            await _mediaService.AttachFileToEntityAsync(categoryDto.IconFile, "Category", id, true);
        }

        await _context.SaveChangesAsync();
        return (true, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> DeleteCategoryAsync(int id)
    {
        var category = await _context.TCategory.Include(c => c.CategoryGroups).ThenInclude(cg => cg.Products).FirstOrDefaultAsync(pt => pt.Id == id);
        if (category == null)
            return (false, "Category not found.");

        if (category.CategoryGroups.Any(cg => cg.Products.Any()))
            return (false, "Cannot delete a category that has associated products in its groups.");

        var media = await _mediaService.GetEntityMediaAsync("Category", id);
        foreach (var m in media)
        {
            await _mediaService.DeleteMediaAsync(m.Id);
        }

        foreach (var group in category.CategoryGroups.ToList())
        {
            var groupMedia = await _mediaService.GetEntityMediaAsync("CategoryGroup", group.Id);
            foreach (var m in groupMedia)
            {
                await _mediaService.DeleteMediaAsync(m.Id);
            }
        }

        _context.TCategory.Remove(category);
        await _context.SaveChangesAsync();

        return (true, null);
    }
}