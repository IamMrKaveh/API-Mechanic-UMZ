namespace Infrastructure.Category.QueryServices;

public class CategoryQueryService(DBContext context, IUrlResolverService urlResolver) : ICategoryQueryService
{
    private readonly DBContext _context = context;
    private readonly IUrlResolverService _urlResolver = urlResolver;

    public async Task<(IEnumerable<Domain.Category.Aggregates.Category> Items, int TotalCount)> GetPagedAsync(
        string? search,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Categories
            .AsNoTracking()
            .Include(c => c.Brands.Where(g => !g.IsDeleted))
                .ThenInclude(b => b.Products.Where(p => !p.IsDeleted))
            .AsQueryable();

        if (!includeDeleted)
            query = query.Where(c => !c.IsDeleted);
        else
            query = query.IgnoreQueryFilters();

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();
            query = query.Where(c => c.Name.Value.ToLower().Contains(searchTerm));
        }

        var totalItems = await query.CountAsync(ct);

        var categories = await query
            .OrderBy(c => c.SortOrder)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (categories, totalItems);
    }

    public async Task<CategoryDetailDto?> GetCategoryDetailAsync(
        int categoryId,
        CancellationToken ct = default)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Where(c => c.Id == categoryId && !c.IsDeleted)
            .Select(c => new
            {
                c.Id,
                Name = c.Name.Value,
                Slug = c.Slug != null ? c.Slug.Value : null,
                c.Description,
                c.IsActive,
                c.SortOrder,
                c.CreatedAt,
                c.UpdatedAt,
                c.RowVersion,
                Brands = c.Brands.Where(g => !g.IsDeleted).Select(g => new
                {
                    g.Id,
                    Name = g.Name.Value,
                    Slug = g.Slug != null ? g.Slug.Value : null,
                    g.IsActive,
                    g.SortOrder,
                    ProductCount = g.Products.Count(p => !p.IsDeleted),
                    ActiveProductCount = g.Products.Count(p => !p.IsDeleted && p.IsActive)
                }).ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (category == null) return null;

        var categoryMedia = await _context.Medias
            .Where(m => m.EntityType == "Category" && m.EntityId == category.Id && m.IsPrimary && !m.IsDeleted)
            .Select(m => m.FilePath)
            .FirstOrDefaultAsync(ct);

        var brandIds = category.Brands.Select(g => g.Id).ToList();
        var brandMediaMap = await _context.Medias
            .Where(m => m.EntityType == "Brand" && brandIds.Contains(m.EntityId) && m.IsPrimary && !m.IsDeleted)
            .Select(m => new { m.EntityId, m.FilePath })
            .ToDictionaryAsync(m => m.EntityId, m => m.FilePath, ct);

        return new CategoryDetailDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            IconUrl = _urlResolver.ResolveUrl(categoryMedia),
            IsActive = category.IsActive,
            SortOrder = category.SortOrder,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt,
            RowVersion = category.RowVersion.ToBase64(),
            Brands = category.Brands.Select(g => new BrandSummaryDto
            {
                Id = g.Id,
                Name = g.Name,
                Slug = g.Slug,
                IconUrl = _urlResolver.ResolveUrl(brandMediaMap.GetValueOrDefault(g.Id)),
                IsActive = g.IsActive,
                SortOrder = g.SortOrder,
                ProductCount = g.ProductCount,
                ActiveProductCount = g.ActiveProductCount
            }).ToList()
        };
    }

    public async Task<PaginatedResult<CategoryListItemDto>> GetCategoriesPagedAsync(
        string? search,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Categories.AsNoTracking().AsQueryable();

        if (!includeDeleted)
            query = query.Where(c => !c.IsDeleted);
        else
            query = query.IgnoreQueryFilters();

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();
            query = query.Where(c => c.Name.Value.ToLower().Contains(searchTerm));
        }

        var totalItems = await query.CountAsync(ct);

        var categories = await query
            .OrderBy(c => c.SortOrder)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                Name = c.Name.Value,
                Slug = c.Slug != null ? c.Slug.Value : null,
                c.IsActive,
                c.IsDeleted,
                c.SortOrder,
                BrandCount = c.Brands.Count(g => !g.IsDeleted),
                ActiveBrandCount = c.Brands.Count(g => !g.IsDeleted && g.IsActive),
                TotalProductCount = c.Brands.Where(g => !g.IsDeleted).SelectMany(g => g.Products).Count(p => !p.IsDeleted),
                c.CreatedAt,
                c.UpdatedAt,
                c.RowVersion
            })
            .ToListAsync(ct);

        var categoryIds = categories.Select(c => c.Id).ToList();
        var mediaMap = await _context.Medias
            .Where(m => m.EntityType == "Category" && categoryIds.Contains(m.EntityId) && m.IsPrimary && !m.IsDeleted)
            .Select(m => new { m.EntityId, m.FilePath })
            .ToDictionaryAsync(m => m.EntityId, m => m.FilePath, ct);

        var dtos = categories
            .Select(c => new CategoryListItemDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                IconUrl = _urlResolver.ResolveUrl(mediaMap.GetValueOrDefault(c.Id)),
                IsActive = c.IsActive,
                IsDeleted = c.IsDeleted,
                SortOrder = c.SortOrder,
                BrandCount = c.BrandCount,
                ActiveBrandCount = c.ActiveBrandCount,
                TotalProductCount = c.TotalProductCount,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                RowVersion = c.RowVersion.ToBase64()
            })
            .ToList();

        return PaginatedResult<CategoryListItemDto>.Create(dtos, totalItems, page, pageSize);
    }

    public async Task<PaginatedResult<CategoryProductItemDto>> GetCategoryProductsAsync(
        int categoryId,
        bool activeOnly,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Where(p => p.Brand != null && p.Brand.CategoryId == categoryId && !p.IsDeleted);

        if (activeOnly)
            query = query.Where(p => p.IsActive);

        var totalItems = await query.CountAsync(ct);

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                Name = p.Name.Value,
                BrandName = p.Brand != null ? p.Brand.Name.Value : "N/A",
                MinPrice = p.Variants.Where(v => !v.IsDeleted && v.IsActive).Any()
                    ? p.Variants.Where(v => !v.IsDeleted && v.IsActive).Min(v => v.SellingPrice.Amount)
                    : 0m,
                MaxPrice = p.Variants.Where(v => !v.IsDeleted && v.IsActive).Any()
                    ? p.Variants.Where(v => !v.IsDeleted && v.IsActive).Max(v => v.SellingPrice.Amount)
                    : 0m,
                TotalStock = p.Variants.Where(v => !v.IsDeleted && !v.IsUnlimited).Sum(v => v.StockQuantity),
                p.IsActive,
                p.CreatedAt
            })
            .ToListAsync(ct);

        var productIds = products.Select(p => p.Id).ToList();
        var mediaMap = await _context.Medias
            .Where(m => m.EntityType == "Product" && productIds.Contains(m.EntityId) && m.IsPrimary && !m.IsDeleted)
            .Select(m => new { m.EntityId, m.FilePath })
            .ToDictionaryAsync(m => m.EntityId, m => m.FilePath, ct);

        var dtos = products
            .Select(p => new CategoryProductItemDto
            {
                Id = p.Id,
                Name = p.Name,
                IconUrl = _urlResolver.ResolveUrl(mediaMap.GetValueOrDefault(p.Id)),
                BrandName = p.BrandName,
                MinPrice = p.MinPrice,
                MaxPrice = p.MaxPrice,
                TotalStock = p.TotalStock,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            }).ToList();

        return PaginatedResult<CategoryProductItemDto>.Create(dtos, totalItems, page, pageSize);
    }
}