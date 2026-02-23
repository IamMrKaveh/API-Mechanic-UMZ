namespace Infrastructure.Category.Services;

public class CategoryQueryService : ICategoryQueryService
{
    private readonly LedkaContext _context;
    private readonly IUrlResolverService _urlResolver;

    public CategoryQueryService(LedkaContext context, IUrlResolverService urlResolver)
    {
        _context = context;
        _urlResolver = urlResolver;
    }

    public async Task<IReadOnlyList<CategoryTreeDto>> GetCategoryTreeAsync(CancellationToken ct = default)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                c.Id,
                Name = c.Name.Value,
                Slug = c.Slug != null ? c.Slug.Value : null,
                c.SortOrder,
                brands = c.Brands
                    .Where(g => !g.IsDeleted && g.IsActive)
                    .OrderBy(g => g.SortOrder)
                    .Select(g => new
                    {
                        g.Id,
                        Name = g.Name.Value,
                        Slug = g.Slug != null ? g.Slug.Value : null,
                        g.SortOrder,
                        ProductCount = g.Products.Count(p => !p.IsDeleted && p.IsActive)
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        var categoryIds = categories.Select(c => c.Id).ToList();
        var brandIds = categories.SelectMany(c => c.brands).Select(g => g.Id).ToList();

        var categoryMedias = await _context.Medias
            .Where(m => m.EntityType == "Category" && categoryIds.Contains(m.EntityId) && m.IsPrimary && !m.IsDeleted)
            .Select(m => new { m.EntityId, m.FilePath })
            .ToDictionaryAsync(m => m.EntityId, m => m.FilePath, ct);

        var brandMedias = await _context.Medias
            .Where(m => m.EntityType == "Brand" && brandIds.Contains(m.EntityId) && m.IsPrimary && !m.IsDeleted)
            .Select(m => new { m.EntityId, m.FilePath })
            .ToDictionaryAsync(m => m.EntityId, m => m.FilePath, ct);

        return categories.Select(c => new CategoryTreeDto
        {
            Id = c.Id,
            Name = c.Name,
            Slug = c.Slug,
            IconUrl = _urlResolver.ResolveUrl(categoryMedias.GetValueOrDefault(c.Id)),
            SortOrder = c.SortOrder,
            Brands = c.brands.Select(g => new BrandTreeDto
            {
                Id = g.Id,
                Name = g.Name,
                Slug = g.Slug,
                SortOrder = g.SortOrder,
                ProductCount = g.ProductCount
            }).ToList()
        }).ToList();
    }

    public async Task<CategoryWithBrandsDto?> GetCategoryWithBrandsAsync(int categoryId, CancellationToken ct = default)
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
                c.IsDeleted,
                c.SortOrder,
                c.CreatedAt,
                c.UpdatedAt,
                c.RowVersion,
                brands = c.Brands
                    .Where(g => !g.IsDeleted)
                    .OrderBy(g => g.SortOrder)
                    .Select(g => new
                    {
                        g.Id,
                        Name = g.Name.Value,
                        Slug = g.Slug != null ? g.Slug.Value : null,
                        g.IsActive,
                        g.SortOrder,
                        ProductCount = g.Products.Count(p => !p.IsDeleted),
                        ActiveProductCount = g.Products.Count(p => !p.IsDeleted && p.IsActive)
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (category == null) return null;

        var brandIds = category.brands.Select(g => g.Id).ToList();

        var categoryMedia = await _context.Medias
            .Where(m => m.EntityType == "Category" && m.EntityId == category.Id && m.IsPrimary && !m.IsDeleted)
            .Select(m => m.FilePath)
            .FirstOrDefaultAsync(ct);

        var brandMedias = await _context.Medias
            .Where(m => m.EntityType == "Brand" && brandIds.Contains(m.EntityId) && m.IsPrimary && !m.IsDeleted)
            .Select(m => new { m.EntityId, m.FilePath })
            .ToDictionaryAsync(m => m.EntityId, m => m.FilePath, ct);

        var brandDtos = category.brands.Select(g => new BrandSummaryDto
        {
            Id = g.Id,
            Name = g.Name,
            Slug = g.Slug,
            IconUrl = _urlResolver.ResolveUrl(brandMedias.GetValueOrDefault(g.Id)),
            IsActive = g.IsActive,
            SortOrder = g.SortOrder,
            ProductCount = g.ProductCount,
            ActiveProductCount = g.ActiveProductCount
        }).ToList();

        return new CategoryWithBrandsDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            IconUrl = _urlResolver.ResolveUrl(categoryMedia),
            IsActive = category.IsActive,
            IsDeleted = category.IsDeleted,
            SortOrder = category.SortOrder,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt,
            RowVersion = category.RowVersion.ToBase64(),
            Brands = brandDtos
        };
    }

    public async Task<PaginatedResult<CategoryListItemDto>> GetCategoriesPagedAsync(
        string? search, bool? isActive, bool includeDeleted,
        int page, int pageSize, CancellationToken ct = default)
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
        int categoryId, bool activeOnly, int page, int pageSize, CancellationToken ct = default)
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

    public async Task<BrandDetailDto?> GetBrandDetailAsync(int brandId, CancellationToken ct = default)
    {
        var brand = await _context.Brands
            .AsNoTracking()
            .Where(g => g.Id == brandId && !g.IsDeleted)
            .Select(g => new
            {
                g.Id,
                Name = g.Name.Value,
                Slug = g.Slug != null ? g.Slug.Value : null,
                g.Description,
                g.CategoryId,
                CategoryName = g.Category.Name.Value,
                g.IsActive,
                g.IsDeleted,
                g.SortOrder,
                ProductCount = g.Products.Count(p => !p.IsDeleted),
                ActiveProductCount = g.Products.Count(p => !p.IsDeleted && p.IsActive),
                g.CreatedAt,
                g.UpdatedAt,
                g.RowVersion
            })
            .FirstOrDefaultAsync(ct);

        if (brand == null) return null;

        var brandMedia = await _context.Medias
            .Where(m => m.EntityType == "Brand" && m.EntityId == brand.Id && m.IsPrimary && !m.IsDeleted)
            .Select(m => m.FilePath)
            .FirstOrDefaultAsync(ct);

        return new BrandDetailDto
        {
            Id = brand.Id,
            Name = brand.Name,
            Slug = brand.Slug,
            Description = brand.Description,
            IconUrl = _urlResolver.ResolveUrl(brandMedia),
            CategoryId = brand.CategoryId,
            CategoryName = brand.CategoryName,
            IsActive = brand.IsActive,
            IsDeleted = brand.IsDeleted,
            SortOrder = brand.SortOrder,
            ProductCount = brand.ProductCount,
            ActiveProductCount = brand.ActiveProductCount,
            CreatedAt = brand.CreatedAt,
            UpdatedAt = brand.UpdatedAt,
            RowVersion = brand.RowVersion.ToBase64()
        };
    }

    public async Task<PaginatedResult<BrandListItemDto>> GetBrandsPagedAsync(
        int? categoryId, string? search, bool? isActive, bool includeDeleted,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Brands.AsNoTracking().AsQueryable();

        if (!includeDeleted)
            query = query.Where(g => !g.IsDeleted);
        else
            query = query.IgnoreQueryFilters();

        if (categoryId.HasValue)
            query = query.Where(g => g.CategoryId == categoryId.Value);

        if (isActive.HasValue)
            query = query.Where(g => g.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();
            query = query.Where(g => g.Name.Value.ToLower().Contains(searchTerm));
        }

        var totalItems = await query.CountAsync(ct);

        var brands = await query
            .OrderBy(g => g.SortOrder)
            .ThenByDescending(g => g.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new
            {
                g.Id,
                Name = g.Name.Value,
                Slug = g.Slug != null ? g.Slug.Value : null,
                g.CategoryId,
                CategoryName = g.Category.Name.Value,
                g.IsActive,
                g.IsDeleted,
                g.SortOrder,
                ProductCount = g.Products.Count(p => !p.IsDeleted),
                g.CreatedAt,
                g.RowVersion
            })
            .ToListAsync(ct);

        var brandIds = brands.Select(g => g.Id).ToList();
        var mediaMap = await _context.Medias
            .Where(m => m.EntityType == "Brand" && brandIds.Contains(m.EntityId) && m.IsPrimary && !m.IsDeleted)
            .Select(m => new { m.EntityId, m.FilePath })
            .ToDictionaryAsync(m => m.EntityId, m => m.FilePath, ct);

        var dtos = brands.Select(g => new BrandListItemDto
        {
            Id = g.Id,
            Name = g.Name,
            Slug = g.Slug,
            IconUrl = _urlResolver.ResolveUrl(mediaMap.GetValueOrDefault(g.Id)),
            CategoryId = g.CategoryId,
            CategoryName = g.CategoryName,
            IsActive = g.IsActive,
            IsDeleted = g.IsDeleted,
            SortOrder = g.SortOrder,
            ProductCount = g.ProductCount,
            CreatedAt = g.CreatedAt,
            RowVersion = g.RowVersion.ToBase64()
        }).ToList();

        return PaginatedResult<BrandListItemDto>.Create(dtos, totalItems, page, pageSize);
    }
}