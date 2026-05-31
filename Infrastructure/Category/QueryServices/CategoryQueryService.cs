using Application.Category.Contracts;
using Application.Category.Features.Shared;
using Domain.Category.ValueObjects;

namespace Infrastructure.Category.QueryServices;

public sealed class CategoryQueryService(
    DBContext context,
    IUrlResolverService urlResolver) : ICategoryQueryService
{
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
            .Where(m => m.EntityType == "Category" && m.EntityId == category.Id.Value && m.IsPrimary)
            .FirstOrDefaultAsync(ct);

        var brandCount = await context.Brands
            .CountAsync(b => b.CategoryId == categoryId, ct);

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
            UpdatedAt = category.UpdatedAt
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
                MinPrice = p.Variants.Where(v => v.IsActive).Any()
                    ? p.Variants.Where(v => v.IsActive).Min(v => v.SellingPrice.Amount)
                    : 0m,
                MaxPrice = p.Variants.Where(v => v.IsActive).Any()
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

    public Task<IReadOnlyList<CategoryTreeDto>> GetCategoryTreeAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<PaginatedResult<CategoryListItemDto>> GetCategoriesPagedAsync(string? search, bool? isActive, bool includeDeleted, int page, int pageSize, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<CategoryWithBrandsDto?> GetCategoryWithBrandsAsync(CategoryId categoryId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
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

        var dtos = items.Select(MapToDto).ToList();
        return PaginatedResult<CategoryDto>.Create(dtos, total, page, pageSize);
    }

    private static CategoryDto MapToDto(Domain.Category.Aggregates.Category category)
        => new()
        {
            Id = category.Id.Value,
            Name = category.Name.Value,
            Slug = category.Slug?.Value,
            Description = category.Description,
            IsActive = category.IsActive,
            SortOrder = category.SortOrder,
            CreatedAt = category.CreatedAt,
            UpdatedAt = category.UpdatedAt
        };
}