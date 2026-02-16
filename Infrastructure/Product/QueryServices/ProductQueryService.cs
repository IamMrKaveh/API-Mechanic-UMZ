namespace Infrastructure.Product.QueryServices;

public class ProductQueryService : IProductQueryService
{
    private readonly LedkaContext _context;
    private readonly IMediaService _mediaService;
    private readonly IStorageService _storageService;

    public ProductQueryService(
        LedkaContext context,
        IMediaService mediaService,
        IStorageService storageService)
    {
        _context = context;
        _mediaService = mediaService;
        _storageService = storageService;
    }

    public async Task<PaginatedResult<AdminProductListItemDto>> GetAdminProductsAsync(
        AdminProductSearchParams searchParams, CancellationToken ct = default)
    {
        var query = _context.Products
            .Include(p => p.CategoryGroup)
                .ThenInclude(cg => cg!.Category)
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
            .AsNoTracking()
            .AsQueryable();

        if (!searchParams.IncludeDeleted)
            query = query.Where(p => !p.IsDeleted);
        else
            query = query.IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(searchParams.Name))
            query = query.Where(p => p.Name.Contains(searchParams.Name));

        if (searchParams.CategoryId.HasValue)
            query = query.Where(p => p.CategoryGroup!.CategoryId == searchParams.CategoryId.Value);

        if (searchParams.CategoryGroupId.HasValue)
            query = query.Where(p => p.CategoryGroupId == searchParams.CategoryGroupId.Value);

        if (searchParams.IsActive.HasValue)
            query = query.Where(p => p.IsActive == searchParams.IsActive.Value);

        var totalItems = await query.CountAsync(ct);

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((searchParams.Page - 1) * searchParams.PageSize)
            .Take(searchParams.PageSize)
            .ToListAsync(ct);

        var dtos = new List<AdminProductListItemDto>();

        foreach (var p in products)
        {
            var iconUrl = await _mediaService.GetPrimaryImageUrlAsync("Product", p.Id);

            dtos.Add(new AdminProductListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                IsActive = p.IsActive,
                IsDeleted = p.IsDeleted,
                CategoryName = p.CategoryGroup?.Category?.Name ?? "N/A",
                CategoryGroupName = p.CategoryGroup?.Name ?? "N/A",
                IconUrl = iconUrl,
                TotalStock = p.Variants.Where(v => !v.IsUnlimited).Sum(v => v.StockQuantity),
                VariantCount = p.Variants.Count,
                MinPrice = p.Variants.Any() ? p.Variants.Min(v => v.SellingPrice) : 0,
                MaxPrice = p.Variants.Any() ? p.Variants.Max(v => v.SellingPrice) : 0,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            });
        }

        return PaginatedResult<AdminProductListItemDto>.Create(
            dtos, totalItems, searchParams.Page, searchParams.PageSize);
    }

    public async Task<AdminProductDetailDto?> GetAdminProductDetailAsync(
        int productId, CancellationToken ct = default)
    {
        var product = await _context.Products
            .IgnoreQueryFilters()
            .Include(p => p.CategoryGroup)
                .ThenInclude(cg => cg!.Category)
            .Include(p => p.Variants)
                .ThenInclude(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeValue)
                        .ThenInclude(av => av.AttributeType)
            .Include(p => p.Variants)
                .ThenInclude(v => v.ProductVariantShippingMethods)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product == null) return null;

        var iconUrl = await _mediaService.GetPrimaryImageUrlAsync("Product", product.Id);
        var images = await _mediaService.GetEntityMediaAsync("Product", product.Id);
        var variants = await MapVariantsToViewDtoAsync(product.Variants.Where(v => !v.IsDeleted));

        return new AdminProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Sku = product.Sku,
            IsActive = product.IsActive,
            IsDeleted = product.IsDeleted,
            CategoryGroupId = product.CategoryGroupId,
            IconUrl = iconUrl,
            Images = images,
            Variants = variants,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            RowVersion = product.RowVersion != null ? Convert.ToBase64String(product.RowVersion) : null
        };
    }

    public async Task<PaginatedResult<ProductCatalogItemDto>> GetProductCatalogAsync(
        ProductCatalogSearchParams searchParams, CancellationToken ct = default)
    {
        var query = _context.Products
            .Where(p => p.IsActive && !p.IsDeleted)
            .Include(p => p.Variants.Where(v => !v.IsDeleted && v.IsActive))
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchParams.Search))
            query = query.Where(p => p.Name.Contains(searchParams.Search));

        if (searchParams.CategoryId.HasValue)
            query = query.Where(p => p.CategoryGroup!.CategoryId == searchParams.CategoryId.Value);

        if (searchParams.CategoryGroupId.HasValue)
            query = query.Where(p => p.CategoryGroupId == searchParams.CategoryGroupId.Value);

        if (searchParams.MinPrice.HasValue)
            query = query.Where(p => p.MinPrice.Amount >= searchParams.MinPrice.Value);

        if (searchParams.MaxPrice.HasValue)
            query = query.Where(p => p.MaxPrice.Amount <= searchParams.MaxPrice.Value);

        if (searchParams.InStockOnly)
            query = query.Where(p => p.TotalStock > 0 || p.Variants.Any(v => v.IsUnlimited));

        // Sorting
        query = searchParams.SortBy switch
        {
            "price_asc" => query.OrderBy(p => p.MinPrice.Amount),
            "price_desc" => query.OrderByDescending(p => p.MaxPrice.Amount),
            "bestselling" => query.OrderByDescending(p => p.SalesCount),
            _ => query.OrderByDescending(p => p.CreatedAt) // newest
        };

        var totalItems = await query.CountAsync(ct);

        var products = await query
            .Skip((searchParams.Page - 1) * searchParams.PageSize)
            .Take(searchParams.PageSize)
            .ToListAsync(ct);

        var dtos = new List<ProductCatalogItemDto>();

        foreach (var p in products)
        {
            var iconUrl = await _mediaService.GetPrimaryImageUrlAsync("Product", p.Id);
            var activeVariants = p.Variants.Where(v => !v.IsDeleted && v.IsActive).ToList();

            dtos.Add(new ProductCatalogItemDto
            {
                Id = p.Id,
                Name = p.Name,
                IconUrl = iconUrl,
                MinPrice = activeVariants.Any() ? activeVariants.Min(v => v.SellingPrice) : 0,
                MaxPrice = activeVariants.Any() ? activeVariants.Max(v => v.SellingPrice) : 0,
                IsInStock = p.TotalStock > 0 || activeVariants.Any(v => v.IsUnlimited),
                HasDiscount = activeVariants.Any(v => v.HasDiscount),
                MaxDiscountPercentage = activeVariants.Any(v => v.HasDiscount)
                    ? activeVariants.Where(v => v.HasDiscount).Max(v => v.DiscountPercentage)
                    : 0,
                AverageRating = p.AverageRating,
                ReviewCount = p.ReviewCount
            });
        }

        return PaginatedResult<ProductCatalogItemDto>.Create(
            dtos, totalItems, searchParams.Page, searchParams.PageSize);
    }

    public async Task<PublicProductDetailDto?> GetPublicProductDetailAsync(
        int productId, CancellationToken ct = default)
    {
        var product = await _context.Products
            .Where(p => p.IsActive && !p.IsDeleted)
            .Include(p => p.CategoryGroup)
                .ThenInclude(cg => cg!.Category)
            .Include(p => p.Variants.Where(v => !v.IsDeleted && v.IsActive))
                .ThenInclude(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeValue)
                        .ThenInclude(av => av.AttributeType)
            .Include(p => p.Variants.Where(v => !v.IsDeleted && v.IsActive))
                .ThenInclude(v => v.ProductVariantShippingMethods)
            .AsNoTracking()
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product == null) return null;

        var iconUrl = await _mediaService.GetPrimaryImageUrlAsync("Product", product.Id);
        var images = await _mediaService.GetEntityMediaAsync("Product", product.Id);
        var variants = await MapVariantsToViewDtoAsync(product.Variants);

        return new PublicProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Sku = product.Sku,
            CategoryGroupId = product.CategoryGroupId,
            CategoryGroup = product.CategoryGroup != null ? new CategoryGroupInfoDto
            {
                Id = product.CategoryGroup.Id,
                Name = product.CategoryGroup.Name,
                CategoryName = product.CategoryGroup.Category?.Name ?? "N/A"
            } : null,
            IconUrl = iconUrl,
            Images = images,
            Variants = variants,
            MinPrice = product.Variants.Any() ? product.Variants.Min(v => v.SellingPrice) : 0,
            MaxPrice = product.Variants.Any() ? product.Variants.Max(v => v.SellingPrice) : 0,
            TotalStock = product.Variants.Where(v => !v.IsUnlimited).Sum(v => v.StockQuantity),
            HasMultipleVariants = product.Variants.Count > 1,
            AverageRating = product.AverageRating,
            ReviewCount = product.ReviewCount
        };
    }

    public async Task<IEnumerable<ProductVariantViewDto>> GetProductVariantsAsync(
        int productId, bool activeOnly = true, CancellationToken ct = default)
    {
        var query = _context.ProductVariants
            .Where(v => v.ProductId == productId && !v.IsDeleted)
            .Include(v => v.VariantAttributes)
                .ThenInclude(va => va.AttributeValue)
                    .ThenInclude(av => av.AttributeType)
            .Include(v => v.ProductVariantShippingMethods)
            .AsNoTracking()
            .AsQueryable();

        if (activeOnly)
            query = query.Where(v => v.IsActive);

        var variants = await query.ToListAsync(ct);
        return await MapVariantsToViewDtoAsync(variants);
    }

    // ========== Private Helpers ==========

    private async Task<List<ProductVariantViewDto>> MapVariantsToViewDtoAsync(
        IEnumerable<ProductVariant> variants)
    {
        var dtos = new List<ProductVariantViewDto>();

        foreach (var v in variants)
        {
            var variantImages = await _mediaService.GetEntityMediaAsync("ProductVariant", v.Id);

            var attributes = v.VariantAttributes
                .Where(va => va.AttributeValue?.AttributeType != null)
                .ToDictionary(
                    va => va.AttributeValue!.AttributeType.Name.ToLowerInvariant(),
                    va => new AttributeValueDto(
                        va.AttributeValue!.Id,
                        va.AttributeValue.AttributeType.Name,
                        va.AttributeValue.AttributeType.DisplayName,
                        va.AttributeValue.Value,
                        va.AttributeValue.DisplayValue ?? va.AttributeValue.Value,
                        va.AttributeValue.HexCode));

            dtos.Add(new ProductVariantViewDto
            {
                Id = v.Id,
                Sku = v.Sku,
                PurchasePrice = v.PurchasePrice,
                OriginalPrice = v.OriginalPrice,
                SellingPrice = v.SellingPrice,
                Stock = v.StockQuantity,
                IsUnlimited = v.IsUnlimited,
                IsActive = v.IsActive,
                IsInStock = v.IsInStock,
                HasDiscount = v.HasDiscount,
                DiscountPercentage = v.DiscountPercentage,
                Attributes = attributes,
                Images = variantImages,
                RowVersion = v.RowVersion != null ? Convert.ToBase64String(v.RowVersion) : null,
                ShippingMultiplier = v.ShippingMultiplier,
                EnabledShippingMethodIds = v.ProductVariantShippingMethods
                    .Where(sm => sm.IsActive)
                    .Select(sm => sm.ShippingMethodId)
                    .ToList()
            });
        }

        return dtos;
    }

    public async Task<AdminProductViewDto?> GetProductForAdminAsync(int productId, CancellationToken ct = default)
    {
        var detail = await GetAdminProductDetailAsync(productId, ct);
        return detail == null ? null : new AdminProductViewDto { Id = detail.Id, Name = detail.Name, Description = detail.Description, Sku = detail.Sku, IsActive = detail.IsActive, IsDeleted = detail.IsDeleted, CategoryGroupId = detail.CategoryGroupId };
    }

    public async Task<PublicProductViewDto?> GetProductForPublicAsync(int productId, CancellationToken ct = default)
    {
        var detail = await GetPublicProductDetailAsync(productId, ct);
        return detail == null ? null : new PublicProductViewDto { Id = detail.Id, Name = detail.Name, Description = detail.Description, Sku = detail.Sku, CategoryGroupId = detail.CategoryGroupId, MinPrice = detail.MinPrice, MaxPrice = detail.MaxPrice, TotalStock = detail.TotalStock };
    }
}