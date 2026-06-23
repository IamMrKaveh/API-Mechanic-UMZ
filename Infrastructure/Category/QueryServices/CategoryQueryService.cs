using Application.Category.Contracts;
using Application.Category.Features.Shared;
using Application.Common.Interfaces;
using Domain.Category.ValueObjects;

namespace Infrastructure.Category.QueryServices;

public sealed class CategoryQueryService(
    DBContext context,
    IUrlResolverService urlResolver) : ICategoryQueryService
{
    private const string CategoryEntityType = "Category";
    private const string BrandEntityType = "Brand";

    public async Task<CategoryDetailDto?> GetCategoryDetailAsync(
           CategoryId categoryId,
           CancellationToken ct = default)
    {
        var category = await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct);

        if (category is null) return null;

        var media = await context.Medias
            .AsNoTracking()
            .Where(m => m.EntityType == CategoryEntityType && m.EntityId == category.Id.Value && m.IsPrimary)
            .FirstOrDefaultAsync(ct);

        var brandCount = await context.Brands
            .CountAsync(b => b.CategoryId == categoryId, ct);

        var rowVersionBytes = context.Entry(category).Property<byte[]>("RowVersion").CurrentValue;

        return new CategoryDetailDto
        {
            Id = category.Id.Value,
            Name = category.Name.Value,
            Slug = category.Slug?.Value,
            Description = category.Description,
            IsActive = category.IsActive,
            SortOrder = category.SortOrder,
            IconUrl = media is not null ? urlResolver.ResolveMediaUrl(media.Path.Value) : null,
            BrandCount = brandCount,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt,
            RowVersion = rowVersionBytes is not null ? Convert.ToBase64String(rowVersionBytes) : null,
        };
    }

    public async Task<IReadOnlyList<CategoryTreeDto>> GetCategoryTreeAsync(CancellationToken ct = default)
    {
        var categories = await context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name.Value)
            .Select(c => new CategoryTreeDto
            {
                Id = c.Id.Value,
                Name = c.Name.Value,
                Slug = c.Slug.Value,
                IsActive = c.IsActive,
                SortOrder = c.SortOrder
            })
            .ToListAsync(ct);

        return categories;
    }

    public async Task<PaginatedResult<CategoryListItemDto>> GetCategoriesPagedAsync(
            string? search,
            bool? isActive,
            bool includeDeleted,
            int page,
            int pageSize,
            CancellationToken ct = default)
    {
        var query = context.Categories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.Name.Value.ToLower().Contains(term));
        }

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        var total = await query.CountAsync(ct);

        var rawItems = await query
            .OrderBy(c => c.SortOrder)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                Name = c.Name.Value,
                Slug = c.Slug.Value,
                c.IsActive,
                c.SortOrder,
                c.CreatedAt,
                c.UpdatedAt,
                ProductCount = context.Products
                    .Count(p => p.Brand != null && p.Brand.CategoryId == c.Id && p.IsActive && !p.IsDeleted),
                RowVersion = EF.Property<byte[]>(c, "RowVersion"),
            })
            .ToListAsync(ct);

        var categoryIds = rawItems.Select(c => c.Id.Value).ToList();

        var iconPaths = await context.Medias
            .AsNoTracking()
            .Where(m => m.EntityType == CategoryEntityType
                        && categoryIds.Contains(m.EntityId)
                        && m.IsPrimary
                        && m.IsActive)
            .Select(m => new { m.EntityId, Path = m.Path.Value })
            .ToDictionaryAsync(m => m.EntityId, m => m.Path, ct);

        var items = rawItems.Select(c => new CategoryListItemDto
        {
            Id = c.Id.Value,
            Name = c.Name,
            Slug = c.Slug,
            IsActive = c.IsActive,
            IsDeleted = false,
            SortOrder = c.SortOrder,
            ProductCount = c.ProductCount,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            RowVersion = c.RowVersion is not null ? Convert.ToBase64String(c.RowVersion) : null,
            IconUrl = iconPaths.TryGetValue(c.Id.Value, out var iconPath) && !string.IsNullOrEmpty(iconPath)
                ? urlResolver.ResolveMediaUrl(iconPath)
                : null,
        }).ToList();

        return PaginatedResult<CategoryListItemDto>.Create(items, total, page, pageSize);
    }

    public async Task<CategoryWithBrandsDto?> GetCategoryWithBrandsAsync(
                CategoryId categoryId,
                CancellationToken ct = default)
    {
        var result = await context.Categories
            .AsNoTracking()
            .Where(c => c.Id == categoryId)
            .Select(c => new
            {
                c.Id,
                Name = c.Name.Value,
                Slug = c.Slug.Value,
                c.Description,
                c.SortOrder,
                c.IsActive,
                RowVersion = EF.Property<byte[]>(c, "RowVersion"),
            })
            .FirstOrDefaultAsync(ct);

        if (result is null) return null;

        var rawBrands = await context.Brands
            .AsNoTracking()
            .Where(b => b.CategoryId == categoryId && !b.IsDeleted)
            .Select(b => new
            {
                Id = b.Id.Value,
                Name = b.Name.Value,
                Slug = b.Slug != null ? b.Slug.Value : null,
                b.LogoPath,
                b.IsActive
            })
            .ToListAsync(ct);

        var brandIdValues = rawBrands.Select(b => b.Id).ToList();

        var brandLogoPaths = await context.Medias
            .AsNoTracking()
            .Where(m => m.EntityType == BrandEntityType
                        && brandIdValues.Contains(m.EntityId)
                        && m.IsPrimary
                        && m.IsActive)
            .Select(m => new { m.EntityId, Path = m.FilePath })
            .ToDictionaryAsync(m => m.EntityId, m => m.Path, ct);

        var brands = rawBrands.Select(b =>
        {
            var mediaPath = brandLogoPaths.TryGetValue(b.Id, out var p) ? p : null;
            var resolvedLogo = !string.IsNullOrWhiteSpace(mediaPath)
                ? urlResolver.ResolveMediaUrl(mediaPath)
                : urlResolver.ResolveMediaUrl(b.LogoPath ?? string.Empty);

            return new BrandInCategoryDto
            {
                Id = b.Id,
                Name = b.Name,
                Slug = b.Slug,
                LogoPath = resolvedLogo,
                IsActive = b.IsActive
            };
        }).ToList();

        var iconPath = await context.Medias
            .AsNoTracking()
            .Where(m => m.EntityType == CategoryEntityType
                        && m.EntityId == result.Id.Value
                        && m.IsPrimary
                        && m.IsActive)
            .Select(m => m.Path.Value)
            .FirstOrDefaultAsync(ct);

        return new CategoryWithBrandsDto
        {
            Id = result.Id.Value,
            Name = result.Name,
            Slug = result.Slug,
            Description = result.Description,
            IconUrl = !string.IsNullOrEmpty(iconPath) ? urlResolver.ResolveMediaUrl(iconPath) : null,
            SortOrder = result.SortOrder,
            IsActive = result.IsActive,
            RowVersion = result.RowVersion is not null ? Convert.ToBase64String(result.RowVersion) : null,
            Brands = brands,
        };
    }

    public async Task<PaginatedResult<CategoryProductItemDto>> GetCategoryProductsAsync(
        CategoryId categoryId,
        bool activeOnly,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.Products
            .AsNoTracking()
            .Where(p => p.Brand != null && p.Brand.CategoryId == categoryId);

        if (activeOnly)
            query = query.Where(p => p.IsActive);

        var total = await query.CountAsync(ct);

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                Id = p.Id.Value,
                p.Name,
                BrandName = p.Brand != null ? p.Brand.Name.Value : "N/A",
                MinPrice = p.Variants.Any(v => v.IsActive)
                    ? p.Variants.Where(v => v.IsActive).Min(v => v.SellingPrice.Amount)
                    : 0m,
                MaxPrice = p.Variants.Any(v => v.IsActive)
                    ? p.Variants.Where(v => v.IsActive).Max(v => v.SellingPrice.Amount)
                    : 0m,
                p.IsActive,
                p.CreatedAt
            })
            .ToListAsync(ct);

        var dtos = products.Select(p => new CategoryProductItemDto
        {
            Id = p.Id,
            Name = p.Name,
            BrandName = p.BrandName,
            MinPrice = p.MinPrice,
            MaxPrice = p.MaxPrice,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt
        }).ToList();

        return PaginatedResult<CategoryProductItemDto>.Create(dtos, total, page, pageSize);
    }

    public async Task<PaginatedResult<CategoryDto>> GetPublicCategoriesAsync(
        string? search, int page, int pageSize, CancellationToken ct = default)
    {
        var query = context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c => c.Name.Value.ToLower().Contains(term));
        }

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.SortOrder)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var categoryIds = items.Select(c => c.Id.Value).ToList();

        var iconPaths = await context.Medias
            .AsNoTracking()
            .Where(m => m.EntityType == CategoryEntityType
                        && categoryIds.Contains(m.EntityId)
                        && m.IsPrimary
                        && m.IsActive)
            .Select(m => new { m.EntityId, Path = m.Path.Value })
            .ToDictionaryAsync(m => m.EntityId, m => m.Path, ct);

        var dtos = items.Select(category => MapToDto(category, iconPaths, urlResolver)).ToList();
        return PaginatedResult<CategoryDto>.Create(dtos, total, page, pageSize);
    }

    private static CategoryDto MapToDto(
        Domain.Category.Aggregates.Category category,
        IReadOnlyDictionary<Guid, string> iconPaths,
        IUrlResolverService resolver)
    {
        var iconUrl = iconPaths.TryGetValue(category.Id.Value, out var path) && !string.IsNullOrWhiteSpace(path)
            ? resolver.ResolveMediaUrl(path)
            : null;

        return new CategoryDto
        {
            Id = category.Id.Value,
            Name = category.Name.Value,
            Slug = category.Slug.Value,
            Description = category.Description,
            IconUrl = iconUrl,
            IsActive = category.IsActive,
            SortOrder = category.SortOrder,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
    }
}