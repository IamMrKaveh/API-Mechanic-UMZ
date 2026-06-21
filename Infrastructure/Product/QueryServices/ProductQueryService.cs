using Application.Attribute.Features.Shared;
using Application.Product.Contracts;
using Application.Product.Features.Shared;
using Application.Variant.Features.Shared;
using Domain.Brand.ValueObjects;
using Domain.Product.ValueObjects;

namespace Infrastructure.Product.QueryServices;

public sealed class ProductQueryService(
    DBContext context,
    IUrlResolverService urlResolver) : IProductQueryService
{
    public async Task<ProductDetailDto?> GetProductDetailAsync(
        ProductId productId, CancellationToken ct = default)
    {
        var product = await context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
                .ThenInclude(v => v.Attributes)
                    .ThenInclude(va => va.AttributeType)
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
                .ThenInclude(v => v.Attributes)
                    .ThenInclude(va => va.Value)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted, ct);

        if (product is null) return null;

        var primaryImagePath = await GetPrimaryImagePathAsync(productId, ct);

        return new ProductDetailDto
        {
            Id = product.Id.Value,
            Name = product.Name.Value,
            Slug = product.Slug.Value,
            Description = product.Description,
            CategoryId = product.CategoryId.Value,
            CategoryName = product.Category.Name.Value,
            BrandId = product.BrandId.Value,
            BrandName = product.Brand.Name.Value,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            IsDeleted = product.IsDeleted,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            PrimaryImageUrl = primaryImagePath is not null
                ? urlResolver.ResolveMediaUrl(primaryImagePath)
                : null,
            Variants = product.Variants
                .Where(v => !v.IsDeleted)
                .Select(MapToVariantDto)
                .ToList()
        };
    }

    public async Task<AdminProductDetailDto?> GetAdminProductDetailAsync(
        ProductId productId, CancellationToken ct = default)
    {
        var product = await context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Attributes)
                    .ThenInclude(va => va.AttributeType)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Attributes)
                    .ThenInclude(va => va.Value)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product is null) return null;

        var primaryImagePath = await GetPrimaryImagePathAsync(productId, ct);

        return new AdminProductDetailDto
        {
            Id = product.Id.Value,
            Name = product.Name.Value,
            Slug = product.Slug.Value,
            Description = product.Description,
            CategoryId = product.CategoryId.Value,
            CategoryName = product.Category.Name.Value,
            BrandId = product.BrandId.Value,
            BrandName = product.Brand.Name.Value,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            IsDeleted = product.IsDeleted,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            DeletedAt = product.DeletedAt,
            PrimaryImageUrl = primaryImagePath is not null
                ? urlResolver.ResolveMediaUrl(primaryImagePath)
                : null,
            Variants = product.Variants
                .Select(MapToVariantDto)
                .ToList()
        };
    }

    public async Task<PublicProductDetailDto?> GetPublicProductDetailAsync(
        ProductId productId, CancellationToken ct = default)
    {
        var product = await context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Category)
            .Include(p => p.Variants.Where(v => v.IsActive && !v.IsDeleted))
                .ThenInclude(v => v.Attributes)
                    .ThenInclude(va => va.AttributeType)
            .Include(p => p.Variants.Where(v => v.IsActive && !v.IsDeleted))
                .ThenInclude(v => v.Attributes)
                    .ThenInclude(va => va.Value)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive && !p.IsDeleted, ct);

        if (product is null) return null;

        var primaryImagePath = await GetPrimaryImagePathAsync(productId, ct);

        return new PublicProductDetailDto
        {
            Id = product.Id.Value,
            Name = product.Name.Value,
            Slug = product.Slug.Value,
            Description = product.Description,
            CategoryId = product.CategoryId.Value,
            CategoryName = product.Category.Name.Value,
            BrandId = product.BrandId.Value,
            BrandName = product.Brand.Name.Value,
            IsFeatured = product.IsFeatured,
            PrimaryImageUrl = primaryImagePath is not null
                ? urlResolver.ResolveMediaUrl(primaryImagePath)
                : null,
            Variants = product.Variants
                .Where(v => v.IsActive && !v.IsDeleted)
                .Select(MapToVariantDto)
                .ToList()
        };
    }

    public async Task<PaginatedResult<ProductListItemDto>> GetAdminProductsAsync(
        Guid? categoryId,
        Guid? brandId,
        string? search,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = context.Products.AsNoTracking();

        if (!includeDeleted)
            query = query.Where(p => !p.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive);

        if (brandId.HasValue)
        {
            var brandFilter = BrandId.From(brandId.Value);
            query = query.Where(p => p.BrandId == brandFilter);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name.Value, pattern) ||
                EF.Functions.ILike(p.Slug.Value, pattern));
        }

        var total = await query.CountAsync(ct);

        var projected = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                Id = p.Id.Value,
                Name = p.Name.Value,
                Slug = p.Slug.Value,
                BrandId = p.BrandId.Value,
                BrandName = p.Brand.Name.Value,
                CategoryId = p.CategoryId.Value,
                CategoryName = p.Category.Name.Value,
                p.IsActive,
                p.IsFeatured,
                p.IsDeleted,
                MinPrice = p.Variants
                    .Where(v => v.IsActive && !v.IsDeleted)
                    .Min(v => (decimal?)v.SellingPrice.Amount),
                HasStock = p.Variants.Any(v => v.IsActive && !v.IsDeleted),
                PrimaryImagePath = context.Medias
                    .Where(m => m.EntityId == p.Id && m.IsPrimary)
                    .Select(m => m.FilePath)
                    .FirstOrDefault(),
                p.CreatedAt
            })
            .ToListAsync(ct);

        var items = projected
            .Select(p => new ProductListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                BrandId = p.BrandId,
                BrandName = p.BrandName,
                CategoryId = p.CategoryId,
                CategoryName = p.CategoryName,
                IsActive = p.IsActive,
                IsFeatured = p.IsFeatured,
                IsDeleted = p.IsDeleted,
                MinPrice = p.MinPrice,
                HasStock = p.HasStock,
                PrimaryImageUrl = p.PrimaryImagePath is not null
                    ? urlResolver.ResolveMediaUrl(p.PrimaryImagePath)
                    : null,
                CreatedAt = p.CreatedAt
            })
            .ToList();

        return new PaginatedResult<ProductListItemDto>(items, total, page, pageSize);
    }

    public async Task<PaginatedResult<ProductCatalogItemDto>> GetProductCatalogAsync(
        ProductCatalogSearchParams searchParams, CancellationToken ct = default)
    {
        var query = context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted);

        if (searchParams.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == searchParams.CategoryId);
        }

        if (searchParams.BrandId.HasValue)
        {
            query = query.Where(p => p.BrandId == searchParams.BrandId);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.Search))
        {
            var pattern = $"%{searchParams.Search.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.Name, pattern));
        }

        if (searchParams.MinPrice.HasValue)
            query = query.Where(p =>
                p.Variants.Any(v => v.IsActive && !v.IsDeleted
                    && v.SellingPrice.Amount >= searchParams.MinPrice));

        if (searchParams.MaxPrice.HasValue)
            query = query.Where(p =>
                p.Variants.Any(v => v.IsActive && !v.IsDeleted
                    && v.SellingPrice.Amount <= searchParams.MaxPrice));

        if (searchParams.InStockOnly)
            query = query.Where(p =>
                p.Variants.Any(v => v.IsActive && !v.IsDeleted));

        var ordered = searchParams.SortBy?.ToLowerInvariant() switch
        {
            "price_asc" => query.OrderBy(p => p.Variants
                .Where(v => v.IsActive && !v.IsDeleted)
                .Min(v => (decimal?)v.SellingPrice.Amount) ?? decimal.MaxValue),
            "price_desc" => query.OrderByDescending(p => p.Variants
                .Where(v => v.IsActive && !v.IsDeleted)
                .Max(v => (decimal?)v.SellingPrice.Amount) ?? decimal.MinValue),
            "name_asc" => query.OrderBy(p => p.Name.Value),
            "name_desc" => query.OrderByDescending(p => p.Name.Value),
            "featured" => query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAt),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        var total = await query.CountAsync(ct);

        var projected = await ordered
            .Skip((searchParams.Page - 1) * searchParams.PageSize)
            .Take(searchParams.PageSize)
            .Select(p => new
            {
                Id = p.Id.Value,
                Name = p.Name.Value,
                Slug = p.Slug.Value,
                BrandId = p.BrandId.Value,
                BrandName = p.Brand.Name.Value,
                CategoryId = p.CategoryId.Value,
                CategoryName = p.Category.Name.Value,
                p.IsFeatured,
                MinPrice = p.Variants
                    .Where(v => v.IsActive && !v.IsDeleted)
                    .Min(v => (decimal?)v.SellingPrice.Amount),
                MaxPrice = p.Variants
                    .Where(v => v.IsActive && !v.IsDeleted)
                    .Max(v => (decimal?)v.SellingPrice.Amount),
                HasStock = p.Variants.Any(v => v.IsActive && !v.IsDeleted),
                PrimaryImagePath = context.Medias
                    .Where(m => m.EntityId == p.Id && m.IsPrimary)
                    .Select(m => m.FilePath)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var items = projected
            .Select(p => new ProductCatalogItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                BrandId = p.BrandId,
                BrandName = p.BrandName,
                CategoryId = p.CategoryId,
                CategoryName = p.CategoryName,
                IsFeatured = p.IsFeatured,
                MinPrice = p.MinPrice,
                MaxPrice = p.MaxPrice,
                HasStock = p.HasStock,
                PrimaryImageUrl = p.PrimaryImagePath is not null
                    ? urlResolver.ResolveMediaUrl(p.PrimaryImagePath)
                    : null
            })
            .ToList();

        return new PaginatedResult<ProductCatalogItemDto>(
            items, total, searchParams.Page, searchParams.PageSize);
    }

    private Task<string?> GetPrimaryImagePathAsync(ProductId productId, CancellationToken ct)
        => context.Medias
            .AsNoTracking()
            .Where(m => m.EntityId == productId && m.IsPrimary)
            .Select(m => m.FilePath)
            .FirstOrDefaultAsync(ct);

    private static ProductVariantViewDto MapToVariantDto(
        Domain.Variant.Aggregates.ProductVariant variant)
        => new()
        {
            Id = variant.Id.Value,
            Sku = variant.Sku.Value,
            SellingPrice = variant.SellingPrice.Amount,
            OriginalPrice = variant.OriginalPrice.Amount,
            HasDiscount = variant.IsDiscounted,
            DiscountPercentage = variant.DiscountPercentage ?? 0m,
            IsActive = variant.IsActive,
            Attributes = variant.Attributes
                .ToDictionary(
                    a => a.AttributeType?.Name ?? a.AttributeTypeId.Value.ToString(),
                    a => new AttributeValueDto
                    {
                        Id = a.Value?.Id.Value ?? Guid.Empty,
                        AttributeTypeId = a.AttributeTypeId.Value,
                        Value = a.Value?.Value ?? a.DisplayValue,
                        DisplayValue = a.DisplayValue,
                        HexCode = a.Value?.HexCode,
                        SortOrder = a.Value?.SortOrder ?? 0,
                        IsActive = a.Value?.IsActive ?? true
                    })
        };
}