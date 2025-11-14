namespace Application.Services;

public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IMediaService _mediaService;
    private readonly IInventoryService _inventoryService;
    private readonly ICacheService _cacheService;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;


    public ProductService(
        IProductRepository repository,
        IHtmlSanitizer htmlSanitizer,
        IMediaService mediaService,
        IInventoryService inventoryService,
        ICacheService cacheService,
        IMapper mapper,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _htmlSanitizer = htmlSanitizer;
        _mediaService = mediaService;
        _inventoryService = inventoryService;
        _cacheService = cacheService;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<PagedResultDto<PublicProductViewDto>>> GetProductsAsync(ProductSearchDto search)
    {
        var query = _repository.GetProductsQuery(search.Name, search.CategoryId, search.MinPrice, search.MaxPrice, search.InStock, search.HasDiscount, search.IsUnlimited, search.SortBy);
        var totalItems = await _repository.GetProductCountAsync(query);
        var items = await _repository.GetPaginatedProductsAsync(query, search.Page, search.PageSize);

        var dtos = new List<PublicProductViewDto>();
        foreach (var p in items)
        {
            dtos.Add(_mapper.Map<PublicProductViewDto>(p));
        }

        var pagedResult = new PagedResultDto<PublicProductViewDto>
        {
            Items = dtos,
            TotalItems = totalItems,
            Page = search.Page,
            PageSize = search.PageSize
        };

        return ServiceResult<PagedResultDto<PublicProductViewDto>>.Ok(pagedResult);
    }

    public async Task<ServiceResult<object?>> GetProductByIdAsync(int id, bool isAdmin)
    {
        var product = await _repository.GetProductByIdAsync(id);
        if (product == null)
        {
            return ServiceResult<object?>.Fail("Product not found");
        }

        if (isAdmin)
        {
            var adminDto = _mapper.Map<AdminProductViewDto>(product);
            return ServiceResult<object?>.Ok(adminDto);
        }

        var publicDto = _mapper.Map<PublicProductViewDto>(product);
        return ServiceResult<object?>.Ok(publicDto);
    }

    public async Task<ServiceResult<Domain.Product.Product>> CreateProductAsync(ProductDto productDto, int userId)
    {
        try
        {
            var product = _mapper.Map<Domain.Product.Product>(productDto);

            if (productDto.VariantsJson != null)
            {
                var variants = JsonSerializer.Deserialize<List<CreateProductVariantDto>>(productDto.VariantsJson) ?? new List<CreateProductVariantDto>();
                foreach (var variantDto in variants)
                {
                    var newVariant = _mapper.Map<Domain.Product.ProductVariant>(variantDto);
                    product.Variants.Add(newVariant);
                }
            }

            await _repository.AddProductAsync(product);

            foreach (var variant in product.Variants)
            {
                if (variant.Stock > 0)
                {
                    await _inventoryService.LogTransactionAsync(variant.Id, "InitialStock", variant.Stock, null, userId, "Product Creation");
                }
            }

            product.RecalculateAggregates();

            if (productDto.Files != null)
            {
                var fileStreams = productDto.Files.Select(file => (file.OpenReadStream(), file.FileName, file.ContentType, file.Length));
                await _mediaService.UploadFilesAsync(fileStreams, "Product", product.Id, true, null);
            }

            await _unitOfWork.SaveChangesAsync();

            return ServiceResult<Domain.Product.Product>.Ok(product);
        }
        catch (Exception ex)
        {
            // Transaction will be automatically rolled back by DbContext
            return ServiceResult<Domain.Product.Product>.Fail("Failed to create product: " + ex.Message);
        }
    }

    public async Task<ServiceResult> UpdateProductAsync(int id, ProductDto productDto, int userId)
    {
        var existingProduct = await _repository.GetProductByIdAsync(id);
        if (existingProduct == null)
        {
            return ServiceResult.Fail("Product not found");
        }

        _mapper.Map(productDto, existingProduct);

        if (productDto.VariantsJson != null)
        {
            var variantDtos = JsonSerializer.Deserialize<List<CreateProductVariantDto>>(productDto.VariantsJson) ?? new List<CreateProductVariantDto>();
            await UpdateVariants(existingProduct, variantDtos, userId);
        }

        _repository.UpdateProduct(existingProduct);

        existingProduct.RecalculateAggregates();

        if (productDto.Files != null)
        {
            var fileStreams = productDto.Files.Select(file => (file.OpenReadStream(), file.FileName, file.ContentType, file.Length));
            await _mediaService.UploadFilesAsync(fileStreams, "Product", id, true, null);
            await _cacheService.ClearByPrefixAsync("cart:user:");
        }

        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteProductAsync(int id)
    {
        var product = await _repository.GetProductByIdAsync(id);
        if (product == null)
        {
            return ServiceResult.Fail("Product not found");
        }

        if (await _repository.HasOrderHistoryAsync(id))
        {
            return ServiceResult.Fail("Cannot delete product that has order history. Consider deactivating instead.");
        }

        var media = await _mediaService.GetEntityMediaAsync("Product", id);
        foreach (var m in media)
        {
            await _mediaService.DeleteMediaAsync(m.Id);
        }

        _repository.DeleteProduct(product);
        await _unitOfWork.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<(int? newCount, string? message)>> AddStockAsync(int id, ProductStockDto stockDto, int userId)
    {
        if (!stockDto.VariantId.HasValue) return ServiceResult<(int?, string?)>.Fail("VariantId is required.");

        var variant = await _repository.GetVariantByIdAsync(stockDto.VariantId.Value);
        if (variant == null || variant.ProductId != id) return ServiceResult<(int?, string?)>.Fail($"Variant with ID {stockDto.VariantId} for product {id} not found");
        if (variant.IsUnlimited) return ServiceResult<(int?, string?)>.Fail("Cannot change stock for an unlimited product.");

        await _inventoryService.LogTransactionAsync(variant.Id, "Adjustment", stockDto.Quantity, null, userId, "Manual stock addition by admin");

        var product = await _repository.GetProductByIdAsync(id);
        if (product != null)
        {
            product.RecalculateAggregates();
        }

        await _unitOfWork.SaveChangesAsync();
        var newStock = await _inventoryService.GetCurrentStockAsync(variant.Id);

        return ServiceResult<(int?, string?)>.Ok((newStock, "Stock added successfully"));
    }

    public async Task<ServiceResult<(int? newCount, string? message)>> RemoveStockAsync(int id, ProductStockDto stockDto, int userId)
    {
        if (!stockDto.VariantId.HasValue) return ServiceResult<(int?, string?)>.Fail("VariantId is required.");

        var variant = await _repository.GetVariantByIdAsync(stockDto.VariantId.Value);
        if (variant == null || variant.ProductId != id) return ServiceResult<(int?, string?)>.Fail($"Variant with ID {stockDto.VariantId} for product {id} not found");
        if (variant.IsUnlimited) return ServiceResult<(int?, string?)>.Fail("Cannot change stock for an unlimited product.");
        if (variant.Stock < stockDto.Quantity) return ServiceResult<(int?, string?)>.Fail($"Insufficient stock. Current stock: {variant.Stock}, Requested: {stockDto.Quantity}");

        await _inventoryService.LogTransactionAsync(variant.Id, "Adjustment", -stockDto.Quantity, null, userId, "Manual stock removal by admin");

        var product = await _repository.GetProductByIdAsync(id);
        if (product != null)
        {
            product.RecalculateAggregates();
        }

        await _unitOfWork.SaveChangesAsync();
        var newStock = await _inventoryService.GetCurrentStockAsync(variant.Id);

        return ServiceResult<(int?, string?)>.Ok((newStock, "Stock removed successfully"));
    }

    public async Task<ServiceResult<IEnumerable<object>>> GetLowStockProductsAsync(int threshold = 5)
    {
        var variants = await _repository.GetLowStockVariantsAsync(threshold);
        var dtos = variants.Select(v => new
        {
            ProductId = v.Product.Id,
            ProductName = v.Product.Name,
            VariantId = v.Id,
            VariantDisplayName = v.DisplayName,
            Stock = v.Stock,
            Category = v.Product.CategoryGroup.Category?.Name,
            SellingPrice = v.SellingPrice
        });
        return ServiceResult<IEnumerable<object>>.Ok(dtos);
    }

    public async Task<ServiceResult<object>> GetProductStatisticsAsync()
    {
        var totalProducts = await _repository.GetActiveProductCountAsync();
        var totalValue = await _repository.GetTotalInventoryValueAsync();
        var outOfStockCount = await _repository.GetOutOfStockCountAsync();
        var lowStockCount = await _repository.GetLowStockCountAsync(5);

        var stats = new
        {
            TotalProducts = totalProducts,
            TotalInventoryValue = (long)totalValue,
            OutOfStockProducts = outOfStockCount,
            LowStockProducts = lowStockCount
        };
        return ServiceResult<object>.Ok(stats);
    }

    public async Task<ServiceResult<(int updatedCount, string? message)>> BulkUpdatePricesAsync(Dictionary<int, decimal> priceUpdates, bool isPurchasePrice)
    {
        var variantIds = priceUpdates.Keys.ToList();
        var variants = await _repository.GetVariantsByIdsAsync(variantIds);

        if (!variants.Any()) return ServiceResult<(int, string?)>.Fail("No variants found with the provided IDs");

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

        var productIds = variants.Select(v => v.ProductId).Distinct();
        foreach (var productId in productIds)
        {
            var product = await _repository.GetProductByIdAsync(productId);
            if (product != null)
            {
                product.RecalculateAggregates();
            }
        }

        await _unitOfWork.SaveChangesAsync();
        await _cacheService.ClearByPrefixAsync("cart:user:");

        return ServiceResult<(int, string?)>.Ok((updatedCount, $"{updatedCount} variants updated successfully"));
    }

    public async Task<ServiceResult<PagedResultDto<object>>> GetDiscountedProductsAsync(int page, int pageSize, int minDiscount, int maxDiscount, int categoryId)
    {
        var query = _repository.GetDiscountedVariantsQuery(minDiscount, maxDiscount, categoryId);
        var totalItems = await _repository.GetDiscountedVariantsCountAsync(query);
        var items = await _repository.GetPaginatedDiscountedVariantsAsync(query, page, pageSize);

        var productDtos = new List<object>();
        foreach (var v in items)
        {
            productDtos.Add(new
            {
                ProductId = v.Product.Id,
                ProductName = v.Product.Name,
                Icon = await _mediaService.GetPrimaryImageUrlAsync("Product", v.Product.Id),
                VariantId = v.Id,
                VariantDisplayName = v.DisplayName,
                OriginalPrice = v.OriginalPrice,
                SellingPrice = v.SellingPrice,
                DiscountAmount = v.OriginalPrice - v.SellingPrice,
                DiscountPercentage = v.DiscountPercentage,
                Stock = v.Stock,
                IsUnlimited = v.IsUnlimited,
                Category = v.Product.CategoryGroup.Category != null ? new { v.Product.CategoryGroup.Category.Id, v.Product.CategoryGroup.Category.Name } : null
            });
        }

        var pagedResult = new PagedResultDto<object>
        {
            Items = productDtos,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize
        };

        return ServiceResult<PagedResultDto<object>>.Ok(pagedResult);
    }

    public async Task<ServiceResult<(object? result, string? message)>> SetProductDiscountAsync(int id, SetDiscountDto discountDto)
    {
        var variant = await _repository.GetVariantByIdAsync(id);
        if (variant == null) return ServiceResult<(object?, string?)>.Fail($"Variant with ID {id} not found");

        variant.OriginalPrice = discountDto.OriginalPrice;
        variant.SellingPrice = discountDto.DiscountedPrice;

        var product = await _repository.GetProductByIdAsync(variant.ProductId);
        if (product != null)
        {
            product.RecalculateAggregates();
        }

        await _unitOfWork.SaveChangesAsync();
        await _cacheService.ClearByPrefixAsync("cart:user:");

        var result = new
        {
            Message = "Discount applied successfully",
            variant.DiscountPercentage,
            OriginalPrice = variant.OriginalPrice,
            DiscountedPrice = variant.SellingPrice
        };

        return ServiceResult<(object?, string?)>.Ok((result, "Discount applied successfully"));
    }

    public async Task<ServiceResult> RemoveProductDiscountAsync(int id)
    {
        var variant = await _repository.GetVariantByIdAsync(id);
        if (variant == null) return ServiceResult.Fail($"Variant with ID {id} not found");
        if (!variant.HasDiscount) return ServiceResult.Fail("Variant does not have a valid discount to remove");

        variant.OriginalPrice = variant.SellingPrice;

        var product = await _repository.GetProductByIdAsync(variant.ProductId);
        if (product != null)
        {
            product.RecalculateAggregates();
        }

        await _unitOfWork.SaveChangesAsync();
        await _cacheService.ClearByPrefixAsync("cart:user:");

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<object>> GetDiscountStatisticsAsync()
    {
        var totalDiscountedVariants = await _repository.GetTotalDiscountedVariantsCountAsync();
        var averageDiscountPercentage = await _repository.GetAverageDiscountPercentageAsync();
        var totalDiscountValue = await _repository.GetTotalDiscountValueAsync();
        var discountByCategory = await _repository.GetDiscountStatsByCategoryAsync();

        var stats = new
        {
            TotalDiscountedProducts = totalDiscountedVariants,
            AverageDiscountPercentage = Math.Round(averageDiscountPercentage, 2),
            TotalDiscountValue = totalDiscountValue,
            DiscountByCategory = discountByCategory
        };

        return ServiceResult<object>.Ok(stats);
    }

    private async Task UpdateVariants(Domain.Product.Product product, List<CreateProductVariantDto> variantDtos, int userId)
    {
        var variantsToRemove = product.Variants.Where(ev => !variantDtos.Any(dto => dto.Sku == ev.Sku)).ToList();

        foreach (var dto in variantDtos)
        {
            var variantToUpdate = product.Variants.FirstOrDefault(v => v.Sku == dto.Sku);
            if (variantToUpdate != null)
            {
                var oldStock = variantToUpdate.Stock;
                _mapper.Map(dto, variantToUpdate);
                if (oldStock != dto.Stock && !variantToUpdate.IsUnlimited)
                {
                    var notes = $"Stock adjustment during product update for SKU: {variantToUpdate.Sku}";
                    await _inventoryService.AdjustStockAsync(variantToUpdate.Id, dto.Stock, userId, notes);
                }
                variantToUpdate.VariantAttributes.Clear();
                foreach (var attrId in dto.AttributeValueIds)
                {
                    variantToUpdate.VariantAttributes.Add(new Domain.Product.Attribute.ProductVariantAttribute { AttributeValueId = attrId });
                }
            }
            else
            {
                var newVariant = _mapper.Map<Domain.Product.ProductVariant>(dto);
                product.Variants.Add(newVariant);
                await _unitOfWork.SaveChangesAsync();
                if (!newVariant.IsUnlimited && dto.Stock > 0)
                {
                    await _inventoryService.LogTransactionAsync(newVariant.Id, "InitialStock", dto.Stock, null, userId, $"New variant added for product {product.Id}");
                }
            }
        }

        if (variantsToRemove.Any())
        {
            foreach (var v in variantsToRemove)
            {
                product.Variants.Remove(v);
            }
        }
    }
}