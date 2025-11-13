namespace MainApi.Services.Product;

public class ProductService : IProductService
{
    private readonly MechanicContext _context;
    private readonly ILogger<ProductService> _logger;
    private readonly IStorageService _storageService;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IMediaService _mediaService;
    private readonly IInventoryService _inventoryService;
    private readonly ICacheService _cacheService;

    public ProductService(
        MechanicContext context,
        ILogger<ProductService> logger,
        IStorageService storageService,
        IHtmlSanitizer htmlSanitizer,
        IMediaService mediaService,
        IInventoryService inventoryService,
        ICacheService cacheService)
    {
        _context = context;
        _logger = logger;
        _storageService = storageService;
        _htmlSanitizer = htmlSanitizer;
        _mediaService = mediaService;
        _inventoryService = inventoryService;
        _cacheService = cacheService;
    }

    public async Task<(IEnumerable<PublicProductViewDto> products, int totalItems)> GetProductsAsync(ProductSearchDto search)
    {
        var query = _context.TProducts
            .Include(p => p.CategoryGroup.Category)
            .Include(p => p.Variants)
                .ThenInclude(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeValue)
                        .ThenInclude(av => av.AttributeType)
            .Include(p => p.Images)
            .Where(p => p.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search.Name))
        {
            var pattern = $"%{search.Name}%";
            query = query.Where(p => p.Name != null && EF.Functions.ILike(p.Name, pattern));
        }

        if (search.CategoryId.HasValue)
            query = query.Where(p => p.CategoryGroup.CategoryId == search.CategoryId);
        if (search.MinPrice.HasValue)
            query = query.Where(p => p.Variants.Any(v => v.SellingPrice >= search.MinPrice.Value));
        if (search.MaxPrice.HasValue)
            query = query.Where(p => p.Variants.Any(v => v.SellingPrice <= search.MaxPrice.Value));
        if (search.InStock == true)
            query = query.Where(p => p.Variants.Any(v => v.IsUnlimited || v.Stock > 0));
        if (search.HasDiscount == true)
            query = query.Where(p => p.Variants.Any(v => v.OriginalPrice > v.SellingPrice));
        if (search.IsUnlimited.HasValue)
            query = query.Where(p => p.Variants.Any(v => v.IsUnlimited == search.IsUnlimited.Value));

        query = search.SortBy switch
        {
            ProductSortOptions.PriceAsc => query.OrderBy(p => p.MinPrice).ThenByDescending(p => p.Id),
            ProductSortOptions.PriceDesc => query.OrderByDescending(p => p.MinPrice).ThenByDescending(p => p.Id),
            ProductSortOptions.NameAsc => query.OrderBy(p => p.Name).ThenByDescending(p => p.Id),
            ProductSortOptions.NameDesc => query.OrderByDescending(p => p.Name).ThenByDescending(p => p.Id),
            ProductSortOptions.DiscountDesc => query.OrderByDescending(p => p.Variants.Max(v => v.OriginalPrice > 0 ? (1 - v.SellingPrice / v.OriginalPrice) * 100 : 0)).ThenByDescending(p => p.Id),
            ProductSortOptions.DiscountAsc => query.OrderBy(p => p.Variants.Max(v => v.OriginalPrice > 0 ? (1 - v.SellingPrice / v.OriginalPrice) * 100 : 0)).ThenByDescending(p => p.Id),
            ProductSortOptions.Oldest => query.OrderBy(p => p.Id),
            _ => query.OrderByDescending(p => p.Id)
        };

        var totalItems = await query.CountAsync();
        var items = await query
            .Skip((search.Page - 1) * search.PageSize)
            .Take(search.PageSize)
            .ToListAsync();

        var dtos = new List<PublicProductViewDto>();
        foreach (var p in items)
        {
            var productImages = new List<MediaDto>();
            foreach (var img in p.Images)
            {
                productImages.Add(new MediaDto(
                    img.Id,
                    _storageService.GetFileUrl(img.FilePath),
                    img.AltText,
                    img.IsPrimary,
                    img.SortOrder
                ));
            }

            var variantDtos = new List<ProductVariantResponseDto>();
            foreach (var v in p.Variants)
            {
                var variantImages = new List<MediaDto>();
                foreach (var img in v.Images)
                {
                    variantImages.Add(new MediaDto(
                        img.Id,
                        _storageService.GetFileUrl(img.FilePath),
                        img.AltText,
                        img.IsPrimary,
                        img.SortOrder
                    ));
                }
                variantDtos.Add(new ProductVariantResponseDto(
                    v.Id,
                    v.Sku,
                    v.PurchasePrice,
                    v.OriginalPrice,
                    v.SellingPrice,
                    v.Stock,
                    v.IsUnlimited,
                    v.IsInStock,
                    v.DiscountPercentage,
                    variantImages,
                    v.VariantAttributes.ToDictionary(
                        va => va.AttributeValue.AttributeType.Name.ToLower(),
                        va => new AttributeValueDto(
                            va.AttributeValueId,
                            va.AttributeValue.AttributeType.Name,
                            va.AttributeValue.AttributeType.DisplayName,
                            va.AttributeValue.Value,
                            va.AttributeValue.DisplayValue,
                            va.AttributeValue.HexCode
                        ))
                ));
            }

            dtos.Add(new PublicProductViewDto(
                p.Id,
                p.Name,
                await _mediaService.GetPrimaryImageUrlAsync("Product", p.Id),
                p.Description,
                p.Sku,
                p.IsActive,
                p.CategoryGroupId,
                p.CategoryGroup != null ? new { p.CategoryGroup.Id, p.CategoryGroup.Name, CategoryName = p.CategoryGroup.Category.Name } : null,
                variantDtos,
                productImages,
                p.MinPrice,
                p.MaxPrice,
                p.TotalStock,
                p.HasMultipleVariants
            ));
        }

        return (dtos, totalItems);
    }

    public async Task<object?> GetProductByIdAsync(int id, bool isAdmin)
    {
        var product = await _context.TProducts
            .Include(p => p.CategoryGroup.Category)
            .Include(p => p.Variants)
                .ThenInclude(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeValue)
                        .ThenInclude(av => av.AttributeType)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Images)
            .Include(p => p.Images)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return null;

        var productImages = new List<MediaDto>();
        foreach (var img in product.Images)
        {
            productImages.Add(new MediaDto(
                img.Id,
                _storageService.GetFileUrl(img.FilePath),
                img.AltText,
                img.IsPrimary,
                img.SortOrder
            ));
        }

        var variantDtos = new List<ProductVariantResponseDto>();
        foreach (var v in product.Variants)
        {
            var variantImages = new List<MediaDto>();
            foreach (var img in v.Images)
            {
                variantImages.Add(new MediaDto(
                    img.Id,
                    _storageService.GetFileUrl(img.FilePath),
                    img.AltText,
                    img.IsPrimary,
                    img.SortOrder
                ));
            }
            variantDtos.Add(new ProductVariantResponseDto(
                v.Id,
                v.Sku,
                isAdmin ? v.PurchasePrice : 0,
                v.OriginalPrice,
                v.SellingPrice,
                v.Stock,
                v.IsUnlimited,
                v.IsInStock,
                v.DiscountPercentage,
                variantImages,
                v.VariantAttributes.ToDictionary(
                    va => va.AttributeValue.AttributeType.Name.ToLower(),
                    va => new AttributeValueDto(
                        va.AttributeValueId,
                        va.AttributeValue.AttributeType.Name,
                        va.AttributeValue.AttributeType.DisplayName,
                        va.AttributeValue.Value,
                        va.AttributeValue.DisplayValue,
                        va.AttributeValue.HexCode
                    ))
            ));
        }

        var baseDto = new PublicProductViewDto(
            product.Id,
            product.Name,
            await _mediaService.GetPrimaryImageUrlAsync("Product", product.Id),
            product.Description,
            product.Sku,
            product.IsActive,
            product.CategoryGroupId,
            product.CategoryGroup != null ? new { product.CategoryGroup.Id, product.CategoryGroup.Name, CategoryName = product.CategoryGroup.Category.Name } : null,
            variantDtos,
            productImages,
            product.MinPrice,
            product.MaxPrice,
            product.TotalStock,
            product.HasMultipleVariants
        );

        if (isAdmin)
        {
            return new AdminProductViewDto(
                baseDto.Id,
                baseDto.Name,
                baseDto.IconUrl,
                baseDto.Description,
                baseDto.Sku,
                baseDto.IsActive,
                baseDto.CategoryGroupId,
                baseDto.CategoryGroup,
                baseDto.Variants,
                baseDto.Images,
                baseDto.MinPrice,
                baseDto.MaxPrice,
                baseDto.TotalStock,
                baseDto.HasMultipleVariants,
                product.RowVersion
            );
        }

        return baseDto;
    }

    public async Task<TProducts> CreateProductAsync(ProductDto productDto, int userId)
    {
        var product = new TProducts
        {
            Name = _htmlSanitizer.Sanitize(productDto.Name),
            Sku = productDto.Sku,
            Description = _htmlSanitizer.Sanitize(productDto.Description ?? ""),
            IsActive = productDto.IsActive,
            CategoryGroupId = productDto.CategoryGroupId
        };

        if (productDto.VariantsJson != null)
        {
            var variants = JsonSerializer.Deserialize<List<CreateProductVariantDto>>(productDto.VariantsJson) ?? new List<CreateProductVariantDto>();
            foreach (var variantDto in variants)
            {
                var newVariant = new TProductVariant
                {
                    Sku = variantDto.Sku,
                    PurchasePrice = variantDto.PurchasePrice,
                    OriginalPrice = variantDto.OriginalPrice,
                    SellingPrice = variantDto.SellingPrice,
                    IsUnlimited = variantDto.IsUnlimited,
                    Stock = variantDto.IsUnlimited ? 0 : variantDto.Stock,
                    IsActive = variantDto.IsActive
                };

                foreach (var attrId in variantDto.AttributeValueIds)
                {
                    newVariant.VariantAttributes.Add(new TProductVariantAttribute { AttributeValueId = attrId });
                }
                product.Variants.Add(newVariant);
            }
        }

        _context.TProducts.Add(product);
        await _context.SaveChangesAsync();

        foreach (var variant in product.Variants)
        {
            if (variant.Stock > 0)
            {
                await _inventoryService.LogTransactionAsync(variant.Id, "InitialStock", variant.Stock, null, userId, "Product Creation");
            }
        }
        await _context.SaveChangesAsync();
        RecalculateProductAggregates(product);
        await _context.SaveChangesAsync();

        if (productDto.Files != null)
        {
            bool isFirst = true;
            foreach (var file in productDto.Files)
            {
                await _mediaService.AttachFileToEntityAsync(file, "Product", product.Id, isFirst);
                isFirst = false;
            }
        }

        return product;
    }

    public async Task<bool> UpdateProductAsync(int id, ProductDto productDto, int userId)
    {
        var existingProduct = await _context.TProducts.Include(p => p.Variants).ThenInclude(v => v.VariantAttributes).FirstOrDefaultAsync(p => p.Id == id);
        if (existingProduct == null) return false;

        if (productDto.RowVersion != null)
            _context.Entry(existingProduct).Property("RowVersion").OriginalValue = productDto.RowVersion;

        if (productDto.Files != null)
        {
            bool isFirst = true;
            foreach (var file in productDto.Files)
            {
                await _mediaService.AttachFileToEntityAsync(file, "Product", id, isFirst);
                isFirst = false;
            }
            await _cacheService.ClearByPrefixAsync("cart:user:");
        }

        existingProduct.Name = _htmlSanitizer.Sanitize(productDto.Name);
        existingProduct.Sku = productDto.Sku;
        existingProduct.Description = _htmlSanitizer.Sanitize(productDto.Description ?? "");
        existingProduct.IsActive = productDto.IsActive;
        existingProduct.CategoryGroupId = productDto.CategoryGroupId;

        if (productDto.VariantsJson != null)
        {
            var variantDtos = JsonSerializer.Deserialize<List<CreateProductVariantDto>>(productDto.VariantsJson) ?? new List<CreateProductVariantDto>();
            var existingVariants = existingProduct.Variants.ToDictionary(v => v.Id, v => v);
            var dtoSkus = variantDtos.Where(d => !string.IsNullOrEmpty(d.Sku)).Select(d => d.Sku).ToHashSet();
            var variantsFromDbBySku = await _context.TProductVariant
                .Where(v => v.ProductId == id && dtoSkus.Contains(v.Sku))
                .ToDictionaryAsync(v => v.Sku!, v => v);

            var variantsToRemove = existingVariants.Values.Where(ev =>
                !variantDtos.Any(dto => dto.Sku == ev.Sku)
            ).ToList();

            foreach (var dto in variantDtos)
            {
                TProductVariant? variantToUpdate = null;
                if (!string.IsNullOrEmpty(dto.Sku) && variantsFromDbBySku.TryGetValue(dto.Sku, out var foundBySku))
                {
                    variantToUpdate = foundBySku;
                }

                if (variantToUpdate != null)
                {
                    variantToUpdate.PurchasePrice = dto.PurchasePrice;
                    variantToUpdate.OriginalPrice = dto.OriginalPrice;
                    variantToUpdate.SellingPrice = dto.SellingPrice;
                    variantToUpdate.IsUnlimited = dto.IsUnlimited;
                    variantToUpdate.IsActive = dto.IsActive;

                    if (variantToUpdate.Stock != dto.Stock && !variantToUpdate.IsUnlimited)
                    {
                        var notes = $"Stock adjustment during product update for SKU: {variantToUpdate.Sku}";
                        await _inventoryService.AdjustStockAsync(variantToUpdate.Id, dto.Stock, userId, notes);
                    }

                    variantToUpdate.VariantAttributes.Clear();
                    foreach (var attrId in dto.AttributeValueIds)
                    {
                        variantToUpdate.VariantAttributes.Add(new TProductVariantAttribute { AttributeValueId = attrId });
                    }
                }
                else
                {
                    var newVariant = new TProductVariant
                    {
                        ProductId = id,
                        Sku = dto.Sku,
                        PurchasePrice = dto.PurchasePrice,
                        OriginalPrice = dto.OriginalPrice,
                        SellingPrice = dto.SellingPrice,
                        IsUnlimited = dto.IsUnlimited,
                        Stock = 0,
                        IsActive = dto.IsActive
                    };

                    foreach (var attrId in dto.AttributeValueIds)
                    {
                        newVariant.VariantAttributes.Add(new TProductVariantAttribute { AttributeValueId = attrId });
                    }
                    existingProduct.Variants.Add(newVariant);

                    await _context.SaveChangesAsync();

                    if (!newVariant.IsUnlimited && dto.Stock > 0)
                    {
                        await _inventoryService.LogTransactionAsync(newVariant.Id, "InitialStock", dto.Stock, null, userId, $"New variant added for product {id}");
                    }
                }
            }

            if (variantsToRemove.Any())
            {
                _context.TProductVariant.RemoveRange(variantsToRemove);
            }
        }

        await _context.SaveChangesAsync();

        RecalculateProductAggregates(existingProduct);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<(bool success, string? message)> DeleteProductAsync(int id)
    {
        var product = await _context.TProducts.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return (false, $"Product with ID {id} not found");

        var hasOrderHistory = await _context.TOrderItems.AnyAsync(oi => product.Variants.Select(v => v.Id).Contains(oi.VariantId));
        if (hasOrderHistory) return (false, "Cannot delete product that has order history. Consider deactivating instead.");

        foreach (var variant in product.Variants)
        {
            var variantMedia = await _mediaService.GetEntityMediaAsync("ProductVariant", variant.Id);
            foreach (var mediaTemp in variantMedia)
            {
                await _mediaService.DeleteMediaAsync(mediaTemp.Id);
            }
        }

        var media = await _mediaService.GetEntityMediaAsync("Product", id);
        foreach (var m in media)
        {
            await _mediaService.DeleteMediaAsync(m.Id);
        }

        _context.TProducts.Remove(product);
        await _context.SaveChangesAsync();

        return (true, "Product deleted successfully");
    }

    public async Task<(bool success, int? newCount, string? message)> AddStockAsync(int id, ProductStockDto stockDto, int userId)
    {
        if (!stockDto.VariantId.HasValue) return (false, null, "VariantId is required.");

        var variant = await _context.TProductVariant.FirstOrDefaultAsync(v => v.Id == stockDto.VariantId && v.ProductId == id);
        if (variant == null) return (false, null, $"Variant with ID {stockDto.VariantId} for product {id} not found");
        if (variant.IsUnlimited) return (false, null, "Cannot change stock for an unlimited product.");

        await _inventoryService.LogTransactionAsync(variant.Id, "Adjustment", stockDto.Quantity, null, userId, "Manual stock addition by admin");
        await _context.SaveChangesAsync();
        var newStock = await _inventoryService.GetCurrentStockAsync(variant.Id);

        var product = await _context.TProducts.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == id);
        if (product != null)
        {
            RecalculateProductAggregates(product);
            await _context.SaveChangesAsync();
        }

        return (true, newStock, "Stock added successfully");
    }

    public async Task<(bool success, int? newCount, string? message)> RemoveStockAsync(int id, ProductStockDto stockDto, int userId)
    {
        if (!stockDto.VariantId.HasValue) return (false, null, "VariantId is required.");

        var variant = await _context.TProductVariant.FirstOrDefaultAsync(v => v.Id == stockDto.VariantId && v.ProductId == id);
        if (variant == null) return (false, null, $"Variant with ID {stockDto.VariantId} for product {id} not found");
        if (variant.IsUnlimited) return (false, null, "Cannot change stock for an unlimited product.");

        if (variant.Stock < stockDto.Quantity) return (false, variant.Stock, $"Insufficient stock. Current stock: {variant.Stock}, Requested: {stockDto.Quantity}");

        await _inventoryService.LogTransactionAsync(variant.Id, "Adjustment", -stockDto.Quantity, null, userId, "Manual stock removal by admin");
        await _context.SaveChangesAsync();
        var newStock = await _inventoryService.GetCurrentStockAsync(variant.Id);

        var product = await _context.TProducts.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == id);
        if (product != null)
        {
            RecalculateProductAggregates(product);
            await _context.SaveChangesAsync();
        }

        return (true, newStock, "Stock removed successfully");
    }

    public async Task<IEnumerable<object>> GetLowStockProductsAsync(int threshold = 5)
    {
        var variants = await _context.TProductVariant
            .Include(v => v.Product.CategoryGroup.Category)
            .Include(v => v.VariantAttributes).ThenInclude(va => va.AttributeValue)
            .Where(v => !v.IsUnlimited && v.Stock <= threshold && v.Stock > 0 && v.IsActive && v.Product.IsActive)
            .OrderBy(v => v.Stock)
            .Select(v => new
            {
                ProductId = v.Product.Id,
                ProductName = v.Product.Name,
                VariantId = v.Id,
                VariantDisplayName = v.DisplayName,
                Stock = v.Stock,
                Category = v.Product.CategoryGroup.Category != null ? v.Product.CategoryGroup.Category.Name : null,
                SellingPrice = v.SellingPrice
            })
            .ToListAsync();
        return variants;
    }

    public async Task<object> GetProductStatisticsAsync()
    {
        var totalProducts = await _context.TProducts.CountAsync(p => p.IsActive);
        var totalValue = await _context.TProductVariant
            .Where(p => !p.IsUnlimited && p.Stock > 0 && p.IsActive)
            .SumAsync(p => (decimal)p.Stock * p.PurchasePrice);
        var outOfStockCount = await _context.TProducts
            .CountAsync(p => p.IsActive && !p.Variants.Any(v => v.IsUnlimited || v.Stock > 0));
        var lowStockCount = await _context.TProducts
            .CountAsync(p => p.IsActive && p.Variants.Any(v => !v.IsUnlimited && v.Stock > 0 && v.Stock <= 5));

        return new
        {
            TotalProducts = totalProducts,
            TotalInventoryValue = (long)totalValue,
            OutOfStockProducts = outOfStockCount,
            LowStockProducts = lowStockCount
        };
    }

    public async Task<(int updatedCount, string? message)> BulkUpdatePricesAsync(Dictionary<int, decimal> priceUpdates, bool isPurchasePrice)
    {
        var variantIds = priceUpdates.Keys.ToList();
        var variants = await _context.TProductVariant
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync();

        if (!variants.Any()) return (0, "No variants found with the provided IDs");

        var updatedCount = 0;
        foreach (var variant in variants)
        {
            if (priceUpdates.TryGetValue(variant.Id, out var newPrice))
            {
                if (isPurchasePrice)
                    variant.PurchasePrice = newPrice;
                else
                    variant.SellingPrice = newPrice;
                updatedCount++;
            }
        }

        await _context.SaveChangesAsync();

        var productIds = variants.Select(v => v.ProductId).Distinct();
        foreach (var productId in productIds)
        {
            var product = await _context.TProducts.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == productId);
            if (product != null)
            {
                RecalculateProductAggregates(product);
            }
        }
        await _context.SaveChangesAsync();
        await _cacheService.ClearByPrefixAsync("cart:user:");

        return (updatedCount, $"{updatedCount} variants updated successfully");
    }

    public async Task<(IEnumerable<object> products, int totalItems)> GetDiscountedProductsAsync(int page, int pageSize, int minDiscount, int maxDiscount, int categoryId)
    {
        var query = _context.TProductVariant
            .Include(v => v.Product.CategoryGroup.Category)
            .Include(v => v.Product.Images)
            .Where(v => v.HasDiscount && v.IsActive && v.Product.IsActive && (v.IsUnlimited || v.Stock > 0))
            .AsQueryable();

        if (categoryId > 0)
            query = query.Where(v => v.Product.CategoryGroup.CategoryId == categoryId);
        if (minDiscount > 0)
            query = query.Where(v => v.DiscountPercentage >= minDiscount);
        if (maxDiscount > 0)
            query = query.Where(v => v.DiscountPercentage <= maxDiscount);

        var totalItems = await query.CountAsync();

        var items = await query
            .OrderByDescending(v => v.DiscountPercentage)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(v => new
            {
                Product = v.Product,
                Variant = v
            })
            .ToListAsync();

        var productDtos = new List<object>();
        foreach (var p in items)
        {
            productDtos.Add(new
            {
                ProductId = p.Product.Id,
                ProductName = p.Product.Name,
                Icon = await _mediaService.GetPrimaryImageUrlAsync("Product", p.Product.Id),
                VariantId = p.Variant.Id,
                VariantDisplayName = p.Variant.DisplayName,
                OriginalPrice = p.Variant.OriginalPrice,
                SellingPrice = p.Variant.SellingPrice,
                DiscountAmount = p.Variant.OriginalPrice - p.Variant.SellingPrice,
                DiscountPercentage = p.Variant.DiscountPercentage,
                Stock = p.Variant.Stock,
                IsUnlimited = p.Variant.IsUnlimited,
                Category = p.Product.CategoryGroup.Category != null ? new { p.Product.CategoryGroup.Category.Id, p.Product.CategoryGroup.Category.Name } : null
            });
        }

        return (productDtos, totalItems);
    }

    public async Task<(bool success, object? result, string? message)> SetProductDiscountAsync(int id, SetDiscountDto discountDto)
    {
        var variant = await _context.TProductVariant.FindAsync(id);
        if (variant == null) return (false, null, $"Variant with ID {id} not found");

        variant.OriginalPrice = discountDto.OriginalPrice;
        variant.SellingPrice = discountDto.DiscountedPrice;

        await _context.SaveChangesAsync();

        var product = await _context.TProducts.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == variant.ProductId);
        if (product != null)
        {
            RecalculateProductAggregates(product);
            await _context.SaveChangesAsync();
        }

        await _cacheService.ClearByPrefixAsync("cart:user:");

        var result = new
        {
            Message = "Discount applied successfully",
            variant.DiscountPercentage,
            OriginalPrice = variant.OriginalPrice,
            DiscountedPrice = variant.SellingPrice
        };

        return (true, result, "Discount applied successfully");
    }

    public async Task<(bool success, string? message)> RemoveProductDiscountAsync(int id)
    {
        var variant = await _context.TProductVariant.FindAsync(id);
        if (variant == null) return (false, $"Variant with ID {id} not found");
        if (!variant.HasDiscount) return (false, "Variant does not have a valid discount to remove");

        variant.OriginalPrice = variant.SellingPrice;

        await _context.SaveChangesAsync();

        var product = await _context.TProducts.Include(p => p.Variants).FirstOrDefaultAsync(p => p.Id == variant.ProductId);
        if (product != null)
        {
            RecalculateProductAggregates(product);
            await _context.SaveChangesAsync();
        }

        await _cacheService.ClearByPrefixAsync("cart:user:");

        return (true, "Discount removed successfully");
    }

    public async Task<object> GetDiscountStatisticsAsync()
    {
        var totalDiscountedVariants = await _context.TProductVariant
            .CountAsync(v => v.HasDiscount && v.IsActive);

        var averageDiscountPercentage = await _context.TProductVariant
            .Where(v => v.HasDiscount && v.IsActive)
            .Select(v => v.DiscountPercentage)
            .DefaultIfEmpty(0)
            .AverageAsync();

        var totalDiscountValue = await _context.TProductVariant
            .Where(v => v.HasDiscount && !v.IsUnlimited && v.Stock > 0 && v.IsActive)
            .SumAsync(v => (long)(v.OriginalPrice - v.SellingPrice) * v.Stock);

        var discountByCategory = await _context.TProductVariant
            .Include(v => v.Product.CategoryGroup.Category)
            .Where(v => v.HasDiscount && v.IsActive)
            .GroupBy(v => new { v.Product.CategoryGroup.CategoryId, v.Product.CategoryGroup.Category!.Name })
            .Select(g => new
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.Name,
                Count = g.Count(),
                AverageDiscount = g.Average(v => v.DiscountPercentage)
            })
            .ToListAsync();

        return new
        {
            TotalDiscountedProducts = totalDiscountedVariants,
            AverageDiscountPercentage = Math.Round(averageDiscountPercentage, 2),
            TotalDiscountValue = totalDiscountValue,
            DiscountByCategory = discountByCategory
        };
    }

    private void RecalculateProductAggregates(TProducts product)
    {
        var activeVariants = product.Variants.Where(v => v.IsActive).ToList();
        if (activeVariants.Any())
        {
            product.MinPrice = activeVariants.Min(v => v.SellingPrice);
            product.MaxPrice = activeVariants.Max(v => v.SellingPrice);
            product.TotalStock = activeVariants.Where(v => !v.IsUnlimited).Sum(v => v.Stock);
        }
        else
        {
            product.MinPrice = 0;
            product.MaxPrice = 0;
            product.TotalStock = 0;
        }
    }
}