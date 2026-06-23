using Application.Brand.Contracts;
using Application.Brand.Features.Shared;
using Application.Common.Interfaces;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Infrastructure.Brand.QueryServices;

public sealed class BrandQueryService(
    DBContext context,
    IUrlResolverService urlResolver) : IBrandQueryService
{
    private const string BrandEntityType = "Brand";
    private const string ConcurrencyTokenName = "xmin";

    public async Task<BrandDetailDto?> GetBrandDetailAsync(
        BrandId brandId,
        CancellationToken ct = default)
    {
        var brand = await context.Brands
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
                b.UpdatedAt,
                ConcurrencyToken = EF.Property<uint>(b, ConcurrencyTokenName)
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
            .AsNoTracking()
            .Where(m => m.EntityType == BrandEntityType
                        && m.EntityId == brand.Id.Value
                        && m.IsPrimary
                        && m.IsActive)
            .Select(m => m.FilePath)
            .FirstOrDefaultAsync(ct);

        return new BrandDetailDto
        {
            Id = brand.Id.Value,
            Name = brand.Name,
            Slug = brand.Slug,
            Description = brand.Description,
            LogoPath = !string.IsNullOrWhiteSpace(mediaPath)
                ? urlResolver.ResolveMediaUrl(mediaPath)
                : urlResolver.ResolveMediaUrl(brand.LogoPath ?? string.Empty),
            CategoryId = brand.CategoryId,
            CategoryName = category ?? string.Empty,
            IsActive = brand.IsActive,
            ProductCount = productCount,
            ActiveProductCount = activeProductCount,
            CreatedAt = brand.CreatedAt,
            UpdatedAt = brand.UpdatedAt,
            RowVersion = Convert.ToBase64String(BitConverter.GetBytes(brand.ConcurrencyToken))
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

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(b => EF.Functions.ILike(b.Name.Value, pattern)
                                  || (b.Slug != null && EF.Functions.ILike(b.Slug.Value, pattern)));
        }

        if (isActive.HasValue)
            query = query.Where(b => b.IsActive == isActive.Value);

        var totalCount = await query.CountAsync(ct);

        var brands = await query
            .OrderBy(b => b.Name.Value)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new
            {
                b.Id,
                b.Name,
                Slug = b.Slug ?? null,
                b.LogoPath,
                b.CategoryId,
                b.IsActive
            })
            .ToListAsync(ct);

        var categoryIds = brands.Select(b => b.CategoryId).Distinct().ToList();
        var categoryNames = await context.Categories
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var brandIds = brands.Select(b => b.Id).ToList();
        var brandIdValues = brandIds.Select(id => id.Value).ToList();

        var productCounts = await context.Products
            .Where(p => brandIds.Contains(p.BrandId!))
            .GroupBy(p => p.BrandId)
            .Select(g => new { BrandId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.BrandId!, g => g.Count, ct);

        var logoPaths = await context.Medias
            .AsNoTracking()
            .Where(m => m.EntityType == BrandEntityType
                        && brandIdValues.Contains(m.EntityId)
                        && m.IsPrimary
                        && m.IsActive)
            .Select(m => new { m.EntityId, Path = m.FilePath })
            .ToDictionaryAsync(m => m.EntityId, m => m.Path, ct);

        var items = brands.Select(b =>
        {
            var mediaPath = logoPaths.TryGetValue(b.Id.Value, out var p) ? p : null;
            var resolvedLogo = !string.IsNullOrWhiteSpace(mediaPath)
                ? urlResolver.ResolveMediaUrl(mediaPath)
                : urlResolver.ResolveMediaUrl(b.LogoPath ?? string.Empty);

            return new BrandListItemDto
            {
                Id = b.Id.Value,
                Name = b.Name.Value,
                Slug = b.Slug?.Value,
                CategoryId = b.CategoryId,
                CategoryName = categoryNames.TryGetValue(b.CategoryId, out var name) ? name : string.Empty,
                IsActive = b.IsActive,
                ProductCount = productCounts.TryGetValue(b.Id, out var count) ? count : 0,
                LogoPath = resolvedLogo
            };
        }).ToList();

        return new PaginatedResult<BrandListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IReadOnlyList<BrandListItemDto>> GetPublicBrandsAsync(
        CategoryId? categoryId = null,
        CancellationToken ct = default)
    {
        var query = context.Brands
            .AsNoTracking()
            .Where(b => b.IsActive);

        if (categoryId is not null)
            query = query.Where(b => b.CategoryId == categoryId);

        var projected = await query
            .OrderBy(b => b.Name.Value)
            .Select(b => new
            {
                b.Id,
                Name = b.Name.Value,
                Slug = b.Slug != null ? b.Slug.Value : null,
                b.LogoPath,
                CategoryId = b.CategoryId.Value,
                b.IsActive
            })
            .ToListAsync(ct);

        var brandIdValues = projected.Select(b => b.Id.Value).ToList();

        var logoPaths = await context.Medias
            .AsNoTracking()
            .Where(m => m.EntityType == BrandEntityType
                        && brandIdValues.Contains(m.EntityId)
                        && m.IsPrimary
                        && m.IsActive)
            .Select(m => new { m.EntityId, Path = m.FilePath })
            .ToDictionaryAsync(m => m.EntityId, m => m.Path, ct);

        return projected.Select(b =>
        {
            var mediaPath = logoPaths.TryGetValue(b.Id.Value, out var p) ? p : null;
            var resolvedLogo = !string.IsNullOrWhiteSpace(mediaPath)
                ? urlResolver.ResolveMediaUrl(mediaPath)
                : urlResolver.ResolveMediaUrl(b.LogoPath ?? string.Empty);

            return new BrandListItemDto
            {
                Id = b.Id.Value,
                Name = b.Name,
                Slug = b.Slug,
                CategoryId = b.CategoryId,
                CategoryName = string.Empty,
                IsActive = b.IsActive,
                ProductCount = 0,
                LogoPath = resolvedLogo
            };
        }).ToList();
    }
}