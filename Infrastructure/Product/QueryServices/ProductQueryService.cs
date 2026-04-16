using Application.Common.Contracts;
using Application.Product.Contracts;
using Application.Product.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.Variant.Entities;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

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
            .Include(p => p.ProductVariants)
            .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted, ct);

        if (product is null) return null;

        return new ProductDetailDto
        {
            Id = product.Id.Value,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            BrandId = product.BrandId.Value,
            BrandName = product.Brand.Name.Value,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            IsDeleted = product.IsDeleted,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            Variants = product.ProductVariants
                .Where(v => !v.IsDeleted)
                .Select(v => MapToVariantDto(v))
                .ToList()
        };
    }

    public async Task<AdminProductDetailDto?> GetAdminProductDetailAsync(
        ProductId productId, CancellationToken ct = default)
    {
        var product = await context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.ProductVariants)
                .ThenInclude(v => v.VariantAttributes)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product is null) return null;

        var primaryImage = await context.Medias
            .AsNoTracking()
            .Where(m => m.EntityId == productId.Value && m.IsPrimary)
            .Select(m => m.FilePath)
            .FirstOrDefaultAsync(ct);

        return new AdminProductDetailDto
        {
            Id = product.Id.Value,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            BrandId = product.BrandId.Value,
            BrandName = product.Brand.Name.Value,
            CategoryId = product.Brand.CategoryId.Value,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            IsDeleted = product.IsDeleted,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            DeletedAt = product.DeletedAt,
            PrimaryImageUrl = primaryImage is not null
                ? urlResolver.GetFileUrl(primaryImage)
                : null,
            Variants = product.ProductVariants
                .Select(v => MapToVariantDto(v))
                .ToList()
        };
    }

    public async Task<PublicProductDetailDto?> GetPublicProductDetailAsync(
        ProductId productId, CancellationToken ct = default)
    {
        var product = await context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.ProductVariants)
                .ThenInclude(v => v.VariantAttributes)
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive && !p.IsDeleted, ct);

        if (product is null) return null;

        var primaryImage = await context.Medias
            .AsNoTracking()
            .Where(m => m.EntityId == productId.Value && m.IsPrimary)
            .Select(m => m.FilePath)
            .FirstOrDefaultAsync(ct);

        return new PublicProductDetailDto
        {
            Id = product.Id.Value,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            BrandId = product.BrandId.Value,
            BrandName = product.Brand.Name.Value,
            CategoryId = product.Brand.CategoryId.Value,
            IsFeatured = product.IsFeatured,
            PrimaryImageUrl = primaryImage is not null
                ? urlResolver.GetFileUrl(primaryImage)
                : null,
            Variants = product.ProductVariants
                .Where(v => v.IsActive && !v.IsDeleted)
                .Select(v => MapToVariantDto(v))
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
        var query = context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .AsQueryable();

        if (!includeDeleted)
            query = query.Where(p => !p.IsDeleted);

        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);

        if (brandId.HasValue)
            query = query.Where(p => p.BrandId.Value == brandId.Value);

        if (categoryId.HasValue)
            query = query.Where(p => p.Brand.CategoryId.Value == categoryId.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.Name.Contains(search) ||
                p.Slug.Contains(search));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListItemDto
            {
                Id = p.Id.Value,
                Name = p.Name,
                Slug = p.Slug,
                BrandId = p.BrandId.Value,
                BrandName = p.Brand.Name.Value,
                CategoryId = p.Brand.CategoryId.Value,
                IsActive = p.IsActive,
                IsFeatured = p.IsFeatured,
                IsDeleted = p.IsDeleted,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(ct);

        return new PaginatedResult<ProductListItemDto>(items, total, page, pageSize);
    }

    public async Task<PaginatedResult<ProductCatalogItemDto>> GetProductCatalogAsync(
        ProductCatalogSearchParams searchParams, CancellationToken ct = default)
    {
        var query = context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.ProductVariants)
            .Where(p => p.IsActive && !p.IsDeleted)
            .AsQueryable();

        if (searchParams.CategoryId.HasValue)
            query = query.Where(p => p.Brand.CategoryId.Value == searchParams.CategoryId.Value);

        if (searchParams.BrandId.HasValue)
            query = query.Where(p => p.BrandId.Value == searchParams.BrandId.Value);

        if (!string.IsNullOrWhiteSpace(searchParams.Search))
            query = query.Where(p => p.Name.Contains(searchParams.Search));

        if (searchParams.MinPrice.HasValue)
            query = query.Where(p =>
                p.ProductVariants.Any(v => v.IsActive && !v.IsDeleted && v.Price.Amount >= searchParams.MinPrice.Value));

        if (searchParams.MaxPrice.HasValue)
            query = query.Where(p =>
                p.ProductVariants.Any(v => v.IsActive && !v.IsDeleted && v.Price.Amount <= searchParams.MaxPrice.Value));

        if (searchParams.InStockOnly)
            query = query.Where(p =>
                p.ProductVariants.Any(v => v.IsActive && !v.IsDeleted && (v.IsUnlimited || v.StockQuantity > 0)));

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((searchParams.Page - 1) * searchParams.PageSize)
            .Take(searchParams.PageSize)
            .Select(p => new ProductCatalogItemDto
            {
                Id = p.Id.Value,
                Name = p.Name,
                Slug = p.Slug,
                BrandId = p.BrandId.Value,
                BrandName = p.Brand.Name.Value,
                CategoryId = p.Brand.CategoryId.Value,
                IsFeatured = p.IsFeatured,
                HasStock = p.ProductVariants.Any(v =>
                    v.IsActive && !v.IsDeleted && (v.IsUnlimited || v.StockQuantity > 0)),
                MinPrice = p.ProductVariants
                    .Where(v => v.IsActive && !v.IsDeleted)
                    .Min(v => (decimal?)v.Price.Amount)
            })
            .ToListAsync(ct);

        return new PaginatedResult<ProductCatalogItemDto>(items, total, searchParams.Page, searchParams.PageSize);
    }

    private static ProductVariantViewDto MapToVariantDto(Domain.Variant.Aggregates.ProductVariant variant)
        => new()
        {
            Id = variant.Id.Value,
            Sku = variant.Sku?.Value,
            Price = variant.Price.Amount,
            CompareAtPrice = variant.CompareAtPrice?.Amount,
            StockQuantity = variant.StockQuantity,
            IsUnlimited = variant.IsUnlimited,
            IsActive = variant.IsActive,
            Attributes = variant.VariantAttributes
                .ToDictionary(
                    a => a.AttributeTypeName,
                    a => a.AttributeValueName)
        };
}