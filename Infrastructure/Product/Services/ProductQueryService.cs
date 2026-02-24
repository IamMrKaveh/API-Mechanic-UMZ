namespace Infrastructure.Product.Services;

public class ProductQueryService : IProductQueryService
{
    private readonly Persistence.Context.DBContext _context;
    private readonly IUrlResolverService _urlResolver;
    private readonly ISearchService _searchService;

    public ProductQueryService(
        Persistence.Context.DBContext context,
        IUrlResolverService urlResolver,
        ISearchService searchService)
    {
        _context = context;
        _urlResolver = urlResolver;
        _searchService = searchService;
    }

    public async Task<IEnumerable<ProductVariantViewDto>> GetProductVariantsAsync(int productId, bool activeOnly, CancellationToken ct = default)
    {
        var query = _context.Set<ProductVariant>()
            .AsNoTracking()
            .AsSplitQuery()
            .Include(v => v.VariantAttributes)
                .ThenInclude(va => va.AttributeValue)
                    .ThenInclude(av => av.AttributeType)
            .Include(v => v.ProductVariantShippings)
            .Where(v => v.ProductId == productId && !v.IsDeleted);

        if (activeOnly)
            query = query.Where(v => v.IsActive);

        var variants = await query.ToListAsync(ct);

        var result = new List<ProductVariantViewDto>();
        foreach (var v in variants)
        {
            var attributesDict = v.VariantAttributes
                .Where(va => va.AttributeValue != null && va.AttributeValue.AttributeType != null)
                .ToDictionary(
                    va => va.AttributeValue.AttributeType.Name,
                    va => new AttributeValueDto(
                        va.AttributeValue.Id,
                        va.AttributeValue.AttributeType.Name,
                        va.AttributeValue.AttributeType.DisplayName,
                        va.AttributeValue.Value,
                        va.AttributeValue.DisplayValue,
                        va.AttributeValue.HexCode)
                );

            result.Add(new ProductVariantViewDto
            {
                Id = v.Id,
                Sku = v.Sku?.Value,
                PurchasePrice = v.PurchasePrice.Amount,
                OriginalPrice = v.OriginalPrice.Amount,
                SellingPrice = v.SellingPrice.Amount,
                Stock = v.StockQuantity,
                IsUnlimited = v.IsUnlimited,
                IsActive = v.IsActive,
                IsInStock = v.IsInStock,
                HasDiscount = v.HasDiscount,
                DiscountPercentage = v.DiscountPercentage,
                Attributes = attributesDict,
                RowVersion = v.RowVersion != null ? Convert.ToBase64String(v.RowVersion) : null,
                ShippingMultiplier = v.ShippingMultiplier,
                EnabledShippingIds = v.ProductVariantShippings.Where(sm => sm.IsActive).Select(sm => sm.ShippingId).ToList()
            });
        }

        return result;
    }

    public async Task<AdminProductDetailDto?> GetAdminProductDetailAsync(int productId, CancellationToken ct = default)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => new
            {
                p.Id,
                Name = p.Name.Value,
                p.Description,
                p.IsActive,
                p.IsDeleted,
                p.BrandId,
                p.CreatedAt,
                p.UpdatedAt,
                p.RowVersion
            })
            .FirstOrDefaultAsync(ct);

        if (product == null) return null;

        var variants = await GetProductVariantsAsync(productId, false, ct);

        var medias = await _context.Medias
            .Where(m => m.EntityType == "Product" && m.EntityId == productId && !m.IsDeleted)
            .OrderBy(m => m.SortOrder)
            .Select(m => new MediaDto
            {
                Id = m.Id,
                FilePath = m.FilePath,
                FileName = m.FileName,
                FileType = m.FileType,
                EntityType = m.EntityType,
                EntityId = m.EntityId,
                AltText = m.AltText,
                SortOrder = m.SortOrder,
                IsPrimary = m.IsPrimary,
                Url = _urlResolver.ResolveUrl(m.FilePath),
                CreatedAt = m.CreatedAt
            })
            .ToListAsync(ct);

        var primaryMediaUrl = medias.FirstOrDefault(m => m.IsPrimary)?.Url;

        return new AdminProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            IsActive = product.IsActive,
            IsDeleted = product.IsDeleted,
            BrandId = product.BrandId,
            IconUrl = primaryMediaUrl,
            Images = medias,
            Variants = variants,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            RowVersion = product.RowVersion != null ? Convert.ToBase64String(product.RowVersion) : null
        };
    }

    public async Task<PublicProductDetailDto?> GetPublicProductDetailAsync(int productId, CancellationToken ct = default)
    {
        var product = await _context.Products
           .AsNoTracking()
           .Include(p => p.Brand)
               .ThenInclude(cg => cg!.Category)
           .Where(p => p.Id == productId && !p.IsDeleted && p.IsActive)
           .FirstOrDefaultAsync(ct);

        if (product == null) return null;

        var variants = await GetProductVariantsAsync(productId, true, ct);
        if (!variants.Any()) return null;

        var medias = await _context.Medias
            .Where(m => m.EntityType == "Product" && m.EntityId == productId && !m.IsDeleted)
            .OrderBy(m => m.SortOrder)
            .Select(m => new MediaDto
            {
                Id = m.Id,
                FilePath = m.FilePath,
                FileName = m.FileName,
                FileType = m.FileType,
                EntityType = m.EntityType,
                EntityId = m.EntityId,
                AltText = m.AltText,
                SortOrder = m.SortOrder,
                IsPrimary = m.IsPrimary,
                Url = _urlResolver.ResolveUrl(m.FilePath),
                CreatedAt = m.CreatedAt
            })
            .ToListAsync(ct);

        var primaryMediaUrl = medias.FirstOrDefault(m => m.IsPrimary)?.Url;

        BrandInfoDto? cgInfo = null;
        if (product.Brand != null)
        {
            cgInfo = new BrandInfoDto
            {
                Id = product.Brand.Id,
                Name = product.Brand.Name.Value,
                CategoryName = product.Brand.Category?.Name?.Value ?? string.Empty
            };
        }

        var availableVariants = variants.Where(v => v.IsInStock).ToList();
        var minPrice = availableVariants.Any() ? availableVariants.Min(v => v.SellingPrice) : variants.Min(v => v.SellingPrice);
        var maxPrice = availableVariants.Any() ? availableVariants.Max(v => v.SellingPrice) : variants.Max(v => v.SellingPrice);

        return new PublicProductDetailDto
        {
            Id = product.Id,
            Name = product.Name.Value,
            Description = product.Description,
            BrandId = product.BrandId,
            Brand = cgInfo,
            IconUrl = primaryMediaUrl,
            Images = medias,
            Variants = variants,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            TotalStock = product.Stats.TotalStock,
            HasMultipleVariants = variants.Count() > 1,
            AverageRating = product.Stats.AverageRating,
            ReviewCount = product.Stats.ReviewCount
        };
    }

    public async Task<PaginatedResult<AdminProductListItemDto>> GetAdminProductsAsync(
            AdminProductSearchParams searchParams, CancellationToken ct = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Brand)
                .ThenInclude(cg => cg!.Category)
            .AsQueryable();

        if (!searchParams.IncludeDeleted)
            query = query.Where(p => !p.IsDeleted);

        if (searchParams.IsActive.HasValue)
            query = query.Where(p => p.IsActive == searchParams.IsActive.Value);

        if (searchParams.CategoryId.HasValue)
            query = query.Where(p => p.Brand != null && p.Brand.CategoryId == searchParams.CategoryId);

        if (searchParams.BrandId.HasValue)
            query = query.Where(p => p.BrandId == searchParams.BrandId);

        if (!string.IsNullOrWhiteSpace(searchParams.Name))
        {
            var term = searchParams.Name.ToLower();
            query = query.Where(p => p.Name.Value.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(ct);

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((searchParams.Page - 1) * searchParams.PageSize)
            .Take(searchParams.PageSize)
            .Select(p => new
            {
                p.Id,
                Name = p.Name.Value,
                p.IsActive,
                p.IsDeleted,
                CategoryName = p.Brand != null && p.Brand.Category != null ? p.Brand.Category.Name.Value : "N/A",
                BrandName = p.Brand != null ? p.Brand.Name.Value : "N/A",
                p.Stats.TotalStock,
                VariantCount = p.Variants.Count(v => !v.IsDeleted),
                Sku = p.Variants.Where(v => !v.IsDeleted).Select(v => v.Sku.Value).FirstOrDefault() ?? string.Empty,
                MinPrice = p.Stats.MinPrice.Amount,
                MaxPrice = p.Stats.MaxPrice.Amount,
                p.CreatedAt,
                p.UpdatedAt
            })
            .ToListAsync(ct);

        var productIds = products.Select(p => p.Id).ToList();
        var mediaMap = await _context.Medias
            .Where(m => m.EntityType == "Product" && productIds.Contains(m.EntityId) && m.IsPrimary && !m.IsDeleted)
            .Select(m => new { m.EntityId, m.FilePath })
            .ToDictionaryAsync(m => m.EntityId, m => m.FilePath, ct);

        var dtos = products.Select(p => new AdminProductListItemDto
        {
            Id = p.Id,
            Name = p.Name,
            IsActive = p.IsActive,
            IsDeleted = p.IsDeleted,
            CategoryName = p.CategoryName,
            BrandName = p.BrandName,
            IconUrl = _urlResolver.ResolveUrl(mediaMap.GetValueOrDefault(p.Id)),
            TotalStock = p.TotalStock,
            VariantCount = p.VariantCount,
            Sku = p.Sku,
            MinPrice = p.MinPrice,
            MaxPrice = p.MaxPrice,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();

        return PaginatedResult<AdminProductListItemDto>.Create(dtos, totalCount, searchParams.Page, searchParams.PageSize);
    }

    public async Task<PaginatedResult<ProductCatalogItemDto>> GetProductCatalogAsync(ProductCatalogSearchParams searchParams, CancellationToken ct = default)
    {
        var searchResult = await _searchService.SearchProductsAsync(new SearchProductsParams
        {
            Q = searchParams.Search ?? string.Empty,
            CategoryId = searchParams.CategoryId,
            BrandId = searchParams.BrandId,
            MinPrice = searchParams.MinPrice,
            MaxPrice = searchParams.MaxPrice,
            InStockOnly = searchParams.InStockOnly,
            SortBy = searchParams.SortBy,
            Page = searchParams.Page,
            PageSize = searchParams.PageSize
        }, ct);

        var items = searchResult.Items.Select(x => new ProductCatalogItemDto
        {
            Id = x.ProductId,
            Name = x.Name,
            IconUrl = x.ImageUrl,
            MinPrice = x.Price,
            MaxPrice = x.Price,
            IsInStock = x.InStock,
            HasDiscount = x.DiscountedPrice.HasValue,
            MaxDiscountPercentage = x.DiscountPercentage ?? 0,
            AverageRating = x.AverageRating,
            ReviewCount = x.ReviewCount
        }).ToList();

        return PaginatedResult<ProductCatalogItemDto>.Create(items, (int)searchResult.Total, searchParams.Page, searchParams.PageSize);
    }

    public async Task<ProductDto?> GetByIdAsync(int id)
    {
        var product = await _context.Products.AsNoTracking()
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return null;

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name.Value,
            Description = product.Description,
            BrandId = product.BrandId,
            IsActive = product.IsActive,
            RowVersion = product.RowVersion != null ? Convert.ToBase64String(product.RowVersion) : null
        };
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync()
    {
        var products = await _context.Products.AsNoTracking()
            .Include(p => p.Variants)
            .ToListAsync();

        return products.Select(product => new ProductDto
        {
            Id = product.Id,
            Name = product.Name.Value,
            Description = product.Description,
            BrandId = product.BrandId,
            IsActive = product.IsActive,
            RowVersion = product.RowVersion != null ? Convert.ToBase64String(product.RowVersion) : null
        });
    }
}