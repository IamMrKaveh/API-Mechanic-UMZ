using Application.Brand.Contracts;
using Application.Brand.Features.Shared;
using Application.Common.Contracts;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Infrastructure.Persistence.Context;
using SharedKernel.Models;

namespace Infrastructure.Brand.QueryServices;

public class BrandQueryService(
    DBContext context,
    IUrlResolverService urlResolver) : IBrandQueryService
{
    private readonly DBContext _context = context;
    private readonly IUrlResolverService _urlResolver = urlResolver;

    public async Task<BrandDetailDto?> GetBrandDetailAsync(
        BrandId brandId,
        CancellationToken ct = default)
    {
        var brand = await _context.Brands
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(g => g.Id == brandId)
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
        CategoryId? categoryId,
        string? search,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default)
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

    public async Task<bool> ExistsByNameInCategoryAsync(
        BrandName name,
        CategoryId categoryId,
        BrandId? excludeId = null,
        CancellationToken ct = default)
    {
        var query = _context.Brands
            .AsNoTracking()
            .Where(g => g.Name == name && g.CategoryId == categoryId && !g.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(g => g.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task<bool> ExistsBySlugAsync(
        string slug,
        int? excludeId = null,
        CancellationToken ct = default)
    {
        var query = _context.Brands
            .AsNoTracking()
            .Where(g => g.Slug != null && g.Slug.Value == slug && !g.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(g => g.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task<int> CountActiveProductsAsync(
        int brandId,
        CancellationToken ct = default)
    {
        return await _context.Products
            .AsNoTracking()
            .CountAsync(p => p.BrandId == brandId && p.IsActive && !p.IsDeleted, ct);
    }
}