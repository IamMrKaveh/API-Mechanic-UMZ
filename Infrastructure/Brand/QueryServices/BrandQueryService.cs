using Application.Brand.Contracts;
using Application.Brand.Features.Shared;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Infrastructure.Brand.QueryServices;

public sealed class BrandQueryService(
    DBContext context,
    IUrlResolverService urlResolver) : IBrandQueryService
{
    public async Task<BrandDetailDto?> GetBrandDetailAsync(
        BrandId brandId,
        CancellationToken ct = default)
    {
        var brand = await context.Brands
            .AsNoTracking()
            .Where(b => b.Id == brandId)
            .Select(b => new
            {
                b.Id,
                Name = b.Name.Value,
                Slug = b.Slug != null ? b.Slug.Value : null,
                b.Description,
                b.LogoPath,
                CategoryId = b.CategoryId.Value,
                b.IsActive,
                b.CreatedAt,
                b.UpdatedAt
            })
            .FirstOrDefaultAsync(ct);

        if (brand is null) return null;

        var category = await context.Categories
            .AsNoTracking()
            .Where(c => c.Id == CategoryId.From(brand.CategoryId))
            .Select(c => c.Name.Value)
            .FirstOrDefaultAsync(ct);

        var productCount = await context.Products
            .CountAsync(p => p.BrandId == brandId, ct);

        var activeProductCount = await context.Products
            .CountAsync(p => p.BrandId == brandId && p.IsActive, ct);

        var mediaPath = await context.Medias
            .Where(m => m.EntityType == "Brand" && m.IsPrimary && m.IsActive)
            .Select(m => m.FilePath)
            .FirstOrDefaultAsync(ct);

        return new BrandDetailDto
        {
            Id = brand.Id.Value,
            Name = brand.Name,
            Slug = brand.Slug,
            Description = brand.Description,
            LogoPath = mediaPath is not null ? urlResolver.ResolveMediaUrl(mediaPath) : brand.LogoPath,
            CategoryId = brand.CategoryId,
            CategoryName = category ?? string.Empty,
            IsActive = brand.IsActive,
            ProductCount = productCount,
            ActiveProductCount = activeProductCount,
            CreatedAt = brand.CreatedAt,
            UpdatedAt = brand.UpdatedAt
        };
    }

    public async Task<PaginatedResult<BrandListItemDto>> GetBrandsPagedAsync(
        CategoryId? categoryId,
        string? search,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.Brands.AsNoTracking().AsQueryable();

        if (categoryId is not null)
            query = query.Where(b => b.CategoryId == categoryId);

        if (isActive.HasValue)
            query = query.Where(b => b.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();
            query = query.Where(b => EF.Functions.ILike(b.Name.Value, $"%{searchTerm}%"));
        }

        var totalItems = await query.CountAsync(ct);

        var brands = await query
            .Select(b => new
            {
                b.Id,
                Name = b.Name.Value,
                Slug = b.Slug != null ? b.Slug.Value : null,
                CategoryId = b.CategoryId.Value,
                b.IsActive,
                b.LogoPath
            })
            .OrderBy(b => b.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var categoryIds = brands.Select(b => CategoryId.From(b.CategoryId)).Distinct().ToList();
        var categoryNames = await context.Categories
            .Where(c => categoryIds.Contains(c.Id))
            .AsNoTracking()
            .ToDictionaryAsync(c => c.Id.Value, c => c.Name.Value, ct);

        var items = brands.Select(b => new BrandListItemDto
        {
            Id = b.Id.Value,
            Name = b.Name,
            Slug = b.Slug,
            CategoryId = b.CategoryId,
            CategoryName = categoryNames.TryGetValue(b.CategoryId, out var cn) ? cn : string.Empty,
            IsActive = b.IsActive,
            LogoPath = b.LogoPath is not null ? urlResolver.ResolveMediaUrl(b.LogoPath) : null
        }).ToList();

        return new PaginatedResult<BrandListItemDto>
        {
            Items = items,
            TotalCount = totalItems,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<BrandListItemDto>> GetPublicBrandsAsync(
        CategoryId? categoryId = null,
        CancellationToken ct = default)
    {
        var query = context.Brands.AsNoTracking().Where(b => b.IsActive);

        if (categoryId is not null)
            query = query.Where(b => b.CategoryId == categoryId);

        var projected = await query
            .Select(b => new
            {
                b.Id,
                NameValue = b.Name.Value,
                SlugValue = b.Slug.Value,
                CategoryIdValue = b.CategoryId.Value,
                b.IsActive,
                b.LogoPath
            })
            .OrderBy(x => x.NameValue)
            .ToListAsync(ct);

        var categoryIds = projected.Select(b => CategoryId.From(b.CategoryIdValue)).Distinct().ToList();
        var categoryNames = await context.Categories
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .Select(c => new { Id = c.Id.Value, Name = c.Name.Value })
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        return projected.Select(b => new BrandListItemDto
        {
            Id = b.Id.Value,
            Name = b.NameValue,
            Slug = b.SlugValue,
            CategoryId = b.CategoryIdValue,
            CategoryName = categoryNames.TryGetValue(b.CategoryIdValue, out var cn) ? cn : string.Empty,
            IsActive = b.IsActive,
            LogoPath = b.LogoPath is not null ? urlResolver.ResolveMediaUrl(b.LogoPath) : null
        }).ToList().AsReadOnly();
    }
}