namespace Infrastructure.Categories.QueryServices;

/// <summary>
/// سرویس کوئری دسته‌بندی‌ها - مستقیماً DTO برمی‌گرداند.
/// بدون بارگذاری Aggregate - بهینه برای خواندن.
/// </summary>
public class CategoryQueryService : ICategoryQueryService
{
    private readonly LedkaContext _context;
    private readonly IMediaService _mediaService;

    public CategoryQueryService(LedkaContext context, IMediaService mediaService)
    {
        _context = context;
        _mediaService = mediaService;
    }

    public async Task<IReadOnlyList<CategoryTreeDto>> GetCategoryTreeAsync(
        CancellationToken ct = default)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Slug,
                c.SortOrder,
                Groups = c.CategoryGroups
                    .Where(g => !g.IsDeleted && g.IsActive)
                    .OrderBy(g => g.SortOrder)
                    .Select(g => new
                    {
                        g.Id,
                        g.Name,
                        g.Slug,
                        g.SortOrder,
                        ProductCount = g.Products.Count(p => !p.IsDeleted && p.IsActive)
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        var result = new List<CategoryTreeDto>();

        foreach (var c in categories)
        {
            var iconUrl = await _mediaService.GetPrimaryImageUrlAsync("Category", c.Id);

            result.Add(new CategoryTreeDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                IconUrl = iconUrl,
                SortOrder = c.SortOrder,
                Groups = c.Groups.Select(g => new CategoryGroupTreeDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Slug = g.Slug,
                    SortOrder = g.SortOrder,
                    ProductCount = g.ProductCount
                }).ToList()
            });
        }

        return result;
    }

    public async Task<CategoryWithGroupsDto?> GetCategoryWithGroupsAsync(
        int categoryId, CancellationToken ct = default)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Where(c => c.Id == categoryId && !c.IsDeleted)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Slug,
                c.Description,
                c.IsActive,
                c.IsDeleted,
                c.SortOrder,
                c.CreatedAt,
                c.UpdatedAt,
                c.RowVersion,
                Groups = c.CategoryGroups
                    .Where(g => !g.IsDeleted)
                    .OrderBy(g => g.SortOrder)
                    .Select(g => new
                    {
                        g.Id,
                        g.Name,
                        g.Slug,
                        g.IsActive,
                        g.SortOrder,
                        ProductCount = g.Products.Count(p => !p.IsDeleted),
                        ActiveProductCount = g.Products.Count(p => !p.IsDeleted && p.IsActive)
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync(ct);

        if (category == null) return null;

        var iconUrl = await _mediaService.GetPrimaryImageUrlAsync("Category", category.Id);

        var groupDtos = new List<CategoryGroupSummaryDto>();
        foreach (var g in category.Groups)
        {
            var groupIconUrl = await _mediaService.GetPrimaryImageUrlAsync("CategoryGroup", g.Id);
            groupDtos.Add(new CategoryGroupSummaryDto
            {
                Id = g.Id,
                Name = g.Name,
                Slug = g.Slug,
                IconUrl = groupIconUrl,
                IsActive = g.IsActive,
                SortOrder = g.SortOrder,
                ProductCount = g.ProductCount,
                ActiveProductCount = g.ActiveProductCount
            });
        }

        return new CategoryWithGroupsDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Description = category.Description,
            IconUrl = iconUrl,
            IsActive = category.IsActive,
            IsDeleted = category.IsDeleted,
            SortOrder = category.SortOrder,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt,
            RowVersion = category.RowVersion != null
                ? Convert.ToBase64String(category.RowVersion)
                : null,
            Groups = groupDtos
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
                c.Name,
                c.Slug,
                c.IsActive,
                c.IsDeleted,
                c.SortOrder,
                GroupCount = c.CategoryGroups.Count(g => !g.IsDeleted),
                ActiveGroupCount = c.CategoryGroups.Count(g => !g.IsDeleted && g.IsActive),
                TotalProductCount = c.CategoryGroups
                    .Where(g => !g.IsDeleted)
                    .SelectMany(g => g.Products)
                    .Count(p => !p.IsDeleted),
                c.CreatedAt,
                c.UpdatedAt,
                c.RowVersion
            })
            .ToListAsync(ct);

        var dtos = new List<CategoryListItemDto>();
        foreach (var c in categories)
        {
            var iconUrl = await _mediaService.GetPrimaryImageUrlAsync("Category", c.Id);

            dtos.Add(new CategoryListItemDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug,
                IconUrl = iconUrl,
                IsActive = c.IsActive,
                IsDeleted = c.IsDeleted,
                SortOrder = c.SortOrder,
                GroupCount = c.GroupCount,
                ActiveGroupCount = c.ActiveGroupCount,
                TotalProductCount = c.TotalProductCount,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                RowVersion = c.RowVersion != null
                    ? Convert.ToBase64String(c.RowVersion)
                    : null
            });
        }

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
            .Where(p => p.CategoryGroup != null && p.CategoryGroup.CategoryId == categoryId && !p.IsDeleted);

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
                p.Name,
                p.Sku,
                GroupName = p.CategoryGroup != null ? p.CategoryGroup.Name : "N/A",
                MinPrice = p.Variants.Where(v => !v.IsDeleted && v.IsActive).Any()
                    ? p.Variants.Where(v => !v.IsDeleted && v.IsActive).Min(v => v.SellingPrice)
                    : 0m,
                MaxPrice = p.Variants.Where(v => !v.IsDeleted && v.IsActive).Any()
                    ? p.Variants.Where(v => !v.IsDeleted && v.IsActive).Max(v => v.SellingPrice)
                    : 0m,
                TotalStock = p.Variants.Where(v => !v.IsDeleted && !v.IsUnlimited).Sum(v => v.StockQuantity),
                p.IsActive,
                p.CreatedAt
            })
            .ToListAsync(ct);

        var dtos = new List<CategoryProductItemDto>();
        foreach (var p in products)
        {
            var iconUrl = await _mediaService.GetPrimaryImageUrlAsync("Product", p.Id);

            dtos.Add(new CategoryProductItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                IconUrl = iconUrl,
                GroupName = p.GroupName,
                MinPrice = p.MinPrice,
                MaxPrice = p.MaxPrice,
                TotalStock = p.TotalStock,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt
            });
        }

        return PaginatedResult<CategoryProductItemDto>.Create(dtos, totalItems, page, pageSize);
    }

    public async Task<CategoryGroupDetailDto?> GetCategoryGroupDetailAsync(
        int groupId, CancellationToken ct = default)
    {
        var group = await _context.CategoryGroups
            .AsNoTracking()
            .Where(g => g.Id == groupId && !g.IsDeleted)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Slug,
                g.Description,
                g.CategoryId,
                CategoryName = g.Category.Name,
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

        if (group == null) return null;

        var iconUrl = await _mediaService.GetPrimaryImageUrlAsync("CategoryGroup", group.Id);

        return new CategoryGroupDetailDto
        {
            Id = group.Id,
            Name = group.Name,
            Slug = group.Slug,
            Description = group.Description,
            IconUrl = iconUrl,
            CategoryId = group.CategoryId,
            CategoryName = group.CategoryName,
            IsActive = group.IsActive,
            IsDeleted = group.IsDeleted,
            SortOrder = group.SortOrder,
            ProductCount = group.ProductCount,
            ActiveProductCount = group.ActiveProductCount,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt,
            RowVersion = group.RowVersion != null
                ? Convert.ToBase64String(group.RowVersion)
                : null
        };
    }

    public async Task<PaginatedResult<CategoryGroupListItemDto>> GetCategoryGroupsPagedAsync(
        int? categoryId,
        string? search,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _context.CategoryGroups
            .AsNoTracking()
            .Include(g => g.Category)
            .AsQueryable();

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

        var groups = await query
            .OrderBy(g => g.SortOrder)
            .ThenByDescending(g => g.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(g => new
            {
                g.Id,
                g.Name,
                g.Slug,
                g.CategoryId,
                CategoryName = g.Category.Name,
                g.IsActive,
                g.IsDeleted,
                g.SortOrder,
                ProductCount = g.Products.Count(p => !p.IsDeleted),
                g.CreatedAt,
                g.RowVersion
            })
            .ToListAsync(ct);

        var dtos = new List<CategoryGroupListItemDto>();
        foreach (var g in groups)
        {
            var iconUrl = await _mediaService.GetPrimaryImageUrlAsync("CategoryGroup", g.Id);

            dtos.Add(new CategoryGroupListItemDto
            {
                Id = g.Id,
                Name = g.Name,
                Slug = g.Slug,
                IconUrl = iconUrl,
                CategoryId = g.CategoryId,
                CategoryName = g.CategoryName,
                IsActive = g.IsActive,
                IsDeleted = g.IsDeleted,
                SortOrder = g.SortOrder,
                ProductCount = g.ProductCount,
                CreatedAt = g.CreatedAt,
                RowVersion = g.RowVersion != null
                    ? Convert.ToBase64String(g.RowVersion)
                    : null
            });
        }

        return PaginatedResult<CategoryGroupListItemDto>.Create(dtos, totalItems, page, pageSize);
    }
}