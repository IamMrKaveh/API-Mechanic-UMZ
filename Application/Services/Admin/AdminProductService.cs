using Application.Common.Utilities;

namespace Application.Services.Admin;

public class AdminProductService : IAdminProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AdminProductService> _logger;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediaService _mediaService;
    private readonly IStorageService _storageService;
    private readonly ICacheService _cacheService;
    private readonly LedkaContext _context;

    private const string ProductCachePrefix = "product:";
    private const string ProductListCachePrefix = "products:list:";
    private const string ProductTagPrefix = "product_tag:";

    public AdminProductService(
        IProductRepository productRepository,
        IInventoryService inventoryService,
        IAuditService auditService,
        ILogger<AdminProductService> logger,
        IHtmlSanitizer htmlSanitizer,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IMediaService mediaService,
        IStorageService storageService,
        ICacheService cacheService,
        LedkaContext context)
    {
        _productRepository = productRepository;
        _inventoryService = inventoryService;
        _auditService = auditService;
        _logger = logger;
        _htmlSanitizer = htmlSanitizer;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _mediaService = mediaService;
        _storageService = storageService;
        _cacheService = cacheService;
        _context = context;
    }

    public async Task<ServiceResult<PagedResultDto<AdminProductListDto>>> GetProductsAsync(
        string? searchTerm,
        int? categoryId,
        bool? isActive,
        bool includeDeleted,
        int page,
        int pageSize)
    {
        var query = _context.Products.AsQueryable();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }
        else
        {
            query = query.Where(p => !p.IsDeleted);
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(p => p.CategoryGroup.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = PersianTextHelper.Normalize(searchTerm);
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{term}%") || EF.Functions.ILike(p.Sku ?? "", $"%{term}%"));
        }

        var totalItems = await query.CountAsync();

        var products = await query
            .Include(p => p.CategoryGroup)
                .ThenInclude(cg => cg.Category)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new AdminProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                IsActive = p.IsActive,
                IsDeleted = p.IsDeleted,
                CategoryName = p.CategoryGroup.Category.Name,
                CategoryGroupName = p.CategoryGroup.Name,
                IconUrl = p.Images.FirstOrDefault(i => i.IsPrimary) != null ? p.Images.FirstOrDefault(i => i.IsPrimary)!.FilePath : null,
                TotalStock = p.Variants.Sum(v => v.StockQuantity),
                VariantCount = p.Variants.Count,
                MinPrice = p.Variants.Any() ? p.Variants.Min(v => v.SellingPrice) : 0,
                MaxPrice = p.Variants.Any() ? p.Variants.Max(v => v.SellingPrice) : 0,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        // Fix image URLs
        foreach (var p in products)
        {
            if (!string.IsNullOrEmpty(p.IconUrl))
            {
                p.IconUrl = _mediaService.GetUrl(p.IconUrl);
            }
        }

        return ServiceResult<PagedResultDto<AdminProductListDto>>.Ok(new PagedResultDto<AdminProductListDto>
        {
            Items = products,
            TotalItems = totalItems,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ServiceResult<AdminProductViewDto?>> GetAdminProductByIdAsync(int productId)
    {
        var product = await _productRepository.GetByIdWithVariantsAndAttributesAsync(productId, true);
        if (product == null)
        {
            return ServiceResult<AdminProductViewDto?>.Fail("Product not found.");
        }
        var dto = await MapToAdminViewDto(product);
        return ServiceResult<AdminProductViewDto?>.Ok(dto);
    }

    public async Task<ServiceResult<AdminProductViewDto>> CreateProductAsync(ProductDto productDto, int userId)
    {
        if (!string.IsNullOrEmpty(productDto.Sku) && await _productRepository.ProductSkuExistsAsync(productDto.Sku))
        {
            return ServiceResult<AdminProductViewDto>.Fail("Product SKU already exists.");
        }

        var variants = JsonSerializer.Deserialize<List<CreateProductVariantDto>>(
               productDto.VariantsJson,
               new JsonSerializerOptions
               {
                   PropertyNameCaseInsensitive = true,
                   NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
               }) ?? [];

        if (!variants.Any())
        {
            return ServiceResult<AdminProductViewDto>.Fail("Product must have at least one variant.");
        }

        var attributeValidationResult = await ValidateVariantAttributesAsync(variants);
        if (!attributeValidationResult.IsValid)
        {
            return ServiceResult<AdminProductViewDto>.Fail(attributeValidationResult.ErrorMessage!);
        }

        var variantAttributeSets = variants
            .Select(v => new HashSet<int>(v.AttributeValueIds))
            .ToList();

        if (variantAttributeSets.Count != new HashSet<HashSet<int>>(variantAttributeSets, HashSet<int>.CreateSetComparer()).Count)
        {
            return ServiceResult<AdminProductViewDto>.Fail("Duplicate variant combinations detected. Each variant must have a unique set of attributes.");
        }

        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            var uploadedFilePaths = new List<string>();

            try
            {
                var product = _mapper.Map<Product>(productDto);
                product.Description = _htmlSanitizer.Sanitize(productDto.Description ?? string.Empty);

                foreach (var variantDto in variants)
                {
                    var variant = _mapper.Map<ProductVariant>(variantDto);
                    variant.Product = product;
                    variant.StockQuantity = variantDto.Stock;
                    variant.ShippingMultiplier = variantDto.ShippingMultiplier;

                    var attributeValues = await _productRepository.GetAttributeValuesByIdsAsync(variantDto.AttributeValueIds);
                    foreach (var attrValue in attributeValues)
                    {
                        variant.VariantAttributes.Add(new ProductVariantAttribute { AttributeValue = attrValue });
                    }

                    foreach (var methodId in variantDto.EnabledShippingMethodIds)
                    {
                        variant.ProductVariantShippingMethods.Add(new ProductVariantShippingMethod
                        {
                            ShippingMethodId = methodId,
                            IsActive = true
                        });
                    }

                    if (variantDto.Stock > 0)
                    {
                        variant.InventoryTransactions.Add(new InventoryTransaction
                        {
                            TransactionType = "StockIn",
                            QuantityChange = variantDto.Stock,
                            StockBefore = 0,
                            Notes = "Initial stock",
                            UserId = userId
                        });
                    }

                    product.Variants.Add(variant);
                }

                await _productRepository.AddAsync(product);
                product.RecalculateAggregates();
                await _unitOfWork.SaveChangesAsync();

                if (productDto.Images != null)
                {
                    for (int i = 0; i < productDto.Images.Count; i++)
                    {
                        var image = productDto.Images[i];
                        bool isPrimary = false;

                        if (productDto.PrimaryImageIndex.HasValue)
                        {
                            if (productDto.PrimaryImageIndex.Value == i) isPrimary = true;
                        }
                        else if (i == 0)
                        {
                            isPrimary = true;
                        }

                        var media = await _mediaService.AttachFileToEntityAsync(
                            image.OpenReadStream(),
                            image.FileName,
                            image.ContentType,
                            image.Length,
                            "Product",
                            product.Id,
                            isPrimary,
                            product.Name,
                            saveChanges: false);

                        uploadedFilePaths.Add(media.FilePath);
                    }
                    await _unitOfWork.SaveChangesAsync();
                }

                await _auditService.LogProductEventAsync(product.Id, "CreateProduct", $"Product '{product.Name}' created.", userId);
                _logger.LogInformation("Product {ProductId} created by user {UserId}", product.Id, userId);

                await transaction.CommitAsync();

                await InvalidateProductCachesAsync(product.Id, product.CategoryGroupId);

                var loadedProduct = await _productRepository.GetByIdWithVariantsAndAttributesAsync(product.Id, true);

                var resultDto = await MapToAdminViewDto(loadedProduct ?? product);
                return ServiceResult<AdminProductViewDto>.Ok(resultDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                foreach (var path in uploadedFilePaths)
                {
                    try
                    {
                        await _storageService.DeleteFileAsync(path);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogError(deleteEx, "Failed to delete orphaned file {Path}", path);
                    }
                }

                _logger.LogError(ex, "Error creating product");
                return ServiceResult<AdminProductViewDto>.Fail("Failed to create product due to an internal error.");
            }
        });
    }

    public async Task<ServiceResult<AdminProductViewDto>> UpdateProductAsync(int productId, ProductDto productDto, int userId)
    {
        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var product = await _productRepository.GetByIdWithVariantsAndAttributesAsync(productId, true);
                if (product == null)
                {
                    return ServiceResult<AdminProductViewDto>.Fail("Product not found.");
                }

                if (!string.IsNullOrEmpty(productDto.Sku) && await _productRepository.ProductSkuExistsAsync(productDto.Sku, productId))
                {
                    return ServiceResult<AdminProductViewDto>.Fail("Product SKU already exists.");
                }

                if (!string.IsNullOrEmpty(productDto.RowVersion))
                {
                    _productRepository.SetOriginalRowVersion(product, Convert.FromBase64String(productDto.RowVersion));
                }

                if (productDto.DeletedMediaIds != null && productDto.DeletedMediaIds.Any())
                {
                    foreach (var mediaId in productDto.DeletedMediaIds)
                    {
                        await _mediaService.DeleteMediaAsync(mediaId);
                    }
                }

                var oldCategoryGroupId = product.CategoryGroupId;

                _mapper.Map(productDto, product);
                product.Description = _htmlSanitizer.Sanitize(productDto.Description ?? string.Empty);

                var variantDtos = JsonSerializer.Deserialize<List<CreateProductVariantDto>>(
                    productDto.VariantsJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
                    }) ?? [];

                if (variantDtos.Any())
                {
                    var attributeValidationResult = await ValidateVariantAttributesAsync(variantDtos);
                    if (!attributeValidationResult.IsValid)
                    {
                        await transaction.RollbackAsync();
                        return ServiceResult<AdminProductViewDto>.Fail(attributeValidationResult.ErrorMessage!);
                    }
                }

                await UpdateVariantsWithStockAsync(product, variantDtos, userId);

                product.RecalculateAggregates();

                if (productDto.Images != null)
                {
                    foreach (var image in productDto.Images)
                    {
                        await _mediaService.AttachFileToEntityAsync(
                            image.OpenReadStream(),
                            image.FileName,
                            image.ContentType,
                            image.Length,
                            "Product",
                            product.Id,
                            false,
                            product.Name,
                            saveChanges: false);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
                await _auditService.LogProductEventAsync(productId, "UpdateProduct", $"Product '{product.Name}' updated.", userId);

                await transaction.CommitAsync();

                await InvalidateProductCachesAsync(productId, product.CategoryGroupId);
                if (oldCategoryGroupId != product.CategoryGroupId)
                {
                    await InvalidateCategoryGroupCacheAsync(oldCategoryGroupId);
                }

                var updatedProductDto = await MapToAdminViewDto(product);
                return ServiceResult<AdminProductViewDto>.Ok(updatedProductDto);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return ServiceResult<AdminProductViewDto>.Fail("The record was modified by another user. Please refresh and try again.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating product {ProductId}", productId);
                return ServiceResult<AdminProductViewDto>.Fail("An error occurred while updating the product.");
            }
        });
    }

    private async Task<(bool IsValid, string? ErrorMessage)> ValidateVariantAttributesAsync(List<CreateProductVariantDto> variants)
    {
        var allAttributeValueIds = variants.SelectMany(v => v.AttributeValueIds).Distinct().ToList();

        if (!allAttributeValueIds.Any())
        {
            return (true, null);
        }

        var existingAttributeValues = await _context.AttributeValues
            .Where(av => allAttributeValueIds.Contains(av.Id) && !av.IsDeleted && av.IsActive)
            .Include(av => av.AttributeType)
            .ToListAsync();

        var existingIds = existingAttributeValues.Select(av => av.Id).ToHashSet();
        var missingIds = allAttributeValueIds.Where(id => !existingIds.Contains(id)).ToList();

        if (missingIds.Any())
        {
            return (false, $"Invalid or inactive attribute values: {string.Join(", ", missingIds)}");
        }

        foreach (var variantDto in variants)
        {
            var variantAttributeValues = existingAttributeValues
                .Where(av => variantDto.AttributeValueIds.Contains(av.Id))
                .ToList();

            var attributeTypeGroups = variantAttributeValues
                .GroupBy(av => av.AttributeTypeId)
                .ToList();

            var duplicateTypes = attributeTypeGroups.Where(g => g.Count() > 1).ToList();
            if (duplicateTypes.Any())
            {
                var duplicateTypeNames = duplicateTypes
                    .Select(g => g.First().AttributeType.DisplayName)
                    .ToList();
                return (false, $"Each variant can only have one value per attribute type. Duplicate types found: {string.Join(", ", duplicateTypeNames)}");
            }
        }

        return (true, null);
    }

    private async Task UpdateVariantsWithStockAsync(Product product, List<CreateProductVariantDto> variantDtos, int userId)
    {
        var existingVariantIds = product.Variants.Select(v => v.Id).ToList();
        var updatedVariantIds = variantDtos.Where(v => v.Id > 0).Select(v => v.Id!.Value).ToList();

        var variantsToDelete = product.Variants.Where(v => !updatedVariantIds.Contains(v.Id)).ToList();
        foreach (var variant in variantsToDelete)
        {
            variant.IsDeleted = true;
            variant.DeletedAt = DateTime.UtcNow;
        }

        foreach (var dto in variantDtos)
        {
            ProductVariant? variant;
            if (dto.Id > 0)
            {
                variant = product.Variants.FirstOrDefault(v => v.Id == dto.Id);
                if (variant == null) continue;

                int currentStock = variant.StockQuantity;
                int newStock = dto.Stock;
                int diff = newStock - currentStock;

                _mapper.Map(dto, variant);
                variant.ShippingMultiplier = dto.ShippingMultiplier;

                var existingMethods = variant.ProductVariantShippingMethods.ToList();

                var toRemove = existingMethods.Where(x => !dto.EnabledShippingMethodIds.Contains(x.ShippingMethodId)).ToList();
                foreach (var item in toRemove)
                    _context.ProductVariantShippingMethods.Remove(item);

                var existingIds = existingMethods.Select(x => x.ShippingMethodId).ToList();
                var toAdd = dto.EnabledShippingMethodIds.Where(id => !existingIds.Contains(id)).ToList();

                foreach (var methodId in toAdd)
                {
                    variant.ProductVariantShippingMethods.Add(new ProductVariantShippingMethod
                    {
                        ShippingMethodId = methodId,
                        IsActive = true
                    });
                }

                if (diff != 0 && !variant.IsUnlimited)
                {
                    await _inventoryService.LogTransactionAsync(
                        variant.Id,
                        "Adjustment",
                        diff,
                        null,
                        userId,
                        "Admin update adjustment",
                        null,
                        null,
                        saveChanges: false
                    );
                }
            }
            else
            {
                variant = _mapper.Map<ProductVariant>(dto);
                variant.StockQuantity = dto.Stock;
                variant.ShippingMultiplier = dto.ShippingMultiplier;

                foreach (var methodId in dto.EnabledShippingMethodIds)
                {
                    variant.ProductVariantShippingMethods.Add(new ProductVariantShippingMethod
                    {
                        ShippingMethodId = methodId,
                        IsActive = true
                    });
                }

                if (dto.Stock > 0)
                {
                    variant.InventoryTransactions.Add(new InventoryTransaction
                    {
                        TransactionType = "StockIn",
                        QuantityChange = dto.Stock,
                        StockBefore = 0,
                        Notes = "Initial stock (Update)",
                        UserId = userId
                    });
                }
                product.Variants.Add(variant);
            }

            variant.VariantAttributes.Clear();
            var attributeValues = await _context.AttributeValues
                .Where(av => dto.AttributeValueIds.Contains(av.Id))
                .ToListAsync();
            foreach (var attrValue in attributeValues)
            {
                variant.VariantAttributes.Add(new ProductVariantAttribute { AttributeValue = attrValue });
            }
        }
    }

    public async Task<ServiceResult> AddStockAsync(int variantId, int quantity, int userId, string notes)
    {
        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var variant = await _productRepository.GetVariantByIdAsync(variantId);
                if (variant == null) return ServiceResult.Fail("Variant not found.");

                await _inventoryService.LogTransactionAsync(variantId, "StockIn", quantity, null, userId, notes, null, variant.RowVersion, saveChanges: false);

                await _unitOfWork.SaveChangesAsync();
                await _auditService.LogInventoryEventAsync(variant.ProductId, "AddStock", $"Added {quantity} to stock for variant {variantId}.", userId);

                await transaction.CommitAsync();

                await InvalidateProductCachesAsync(variant.ProductId);

                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error adding stock");
                return ServiceResult.Fail(ex.Message);
            }
        });
    }

    public async Task<ServiceResult> RemoveStockAsync(int variantId, int quantity, int userId, string notes)
    {
        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var variant = await _productRepository.GetVariantByIdAsync(variantId);
                if (variant == null) return ServiceResult.Fail("Variant not found.");

                await _inventoryService.LogTransactionAsync(variantId, "StockOut", -quantity, null, userId, notes, null, variant.RowVersion, saveChanges: false);

                await _unitOfWork.SaveChangesAsync();
                await _auditService.LogInventoryEventAsync(variant.ProductId, "RemoveStock", $"Removed {quantity} from stock for variant {variantId}.", userId);

                await transaction.CommitAsync();

                await InvalidateProductCachesAsync(variant.ProductId);

                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error removing stock");
                return ServiceResult.Fail(ex.Message);
            }
        });
    }

    public async Task<ServiceResult> SetDiscountAsync(int variantId, decimal originalPrice, decimal discountedPrice, int userId)
    {
        if (discountedPrice < 0 || originalPrice < 0) return ServiceResult.Fail("Prices cannot be negative.");
        if (discountedPrice >= originalPrice) return ServiceResult.Fail("Discounted price must be less than the original price.");

        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var variant = await _productRepository.GetVariantByIdAsync(variantId);
                if (variant == null) return ServiceResult.Fail("Variant not found.");

                await _auditService.LogProductEventAsync(variant.ProductId, "PriceChange", $"Variant {variantId} price changed from {variant.SellingPrice} (Orig: {variant.OriginalPrice}) to {discountedPrice} (Orig: {originalPrice}).", userId);

                variant.OriginalPrice = originalPrice;
                variant.SellingPrice = discountedPrice;

                _productRepository.UpdateVariant(variant);
                await _unitOfWork.SaveChangesAsync();

                await transaction.CommitAsync();

                await InvalidateProductCachesAsync(variant.ProductId);

                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error setting discount");
                return ServiceResult.Fail("Failed to set discount.");
            }
        });
    }

    public async Task<ServiceResult> RemoveDiscountAsync(int variantId, int userId)
    {
        var variant = await _productRepository.GetVariantByIdAsync(variantId);
        if (variant == null) return ServiceResult.Fail("Variant not found.");

        variant.SellingPrice = variant.OriginalPrice;

        _productRepository.UpdateVariant(variant);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogProductEventAsync(variant.ProductId, "RemoveDiscount", $"Discount removed for variant {variantId}.", userId);

        await InvalidateProductCachesAsync(variant.ProductId);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> BulkUpdatePricesAsync(Dictionary<int, decimal> priceUpdates, bool isPurchasePrice, int userId)
    {
        if (priceUpdates == null || !priceUpdates.Any()) return ServiceResult.Fail("No price updates provided.");

        if (priceUpdates.Any(p => p.Value < 0)) return ServiceResult.Fail("Prices cannot be negative.");

        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var variantIds = priceUpdates.Keys.ToList();
                var variants = await _productRepository.GetVariantsByIdsAsync(variantIds);

                var failedIds = new List<int>();
                var changesLog = new List<string>();
                var affectedProductIds = new HashSet<int>();

                foreach (var (variantId, newPrice) in priceUpdates)
                {
                    if (variants.TryGetValue(variantId, out var variant))
                    {
                        var oldPrice = isPurchasePrice ? variant.PurchasePrice : variant.SellingPrice;

                        if (isPurchasePrice)
                            variant.PurchasePrice = newPrice;
                        else
                            variant.SellingPrice = newPrice;

                        _productRepository.UpdateVariant(variant);
                        changesLog.Add($"Variant {variantId}: {oldPrice} -> {newPrice}");
                        affectedProductIds.Add(variant.ProductId);
                    }
                    else
                    {
                        failedIds.Add(variantId);
                    }
                }

                if (failedIds.Any())
                {
                    _logger.LogWarning("Bulk update partial failure. Variants not found: {Ids}", string.Join(",", failedIds));
                }

                await _unitOfWork.SaveChangesAsync();

                await _auditService.LogSystemEventAsync("BulkPriceUpdate", $"User {userId} updated prices. Changes: {string.Join("; ", changesLog)}", userId);

                await transaction.CommitAsync();

                foreach (var productId in affectedProductIds)
                {
                    await InvalidateProductCachesAsync(productId);
                }

                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error in bulk price update");
                return ServiceResult.Fail("Bulk update failed.");
            }
        });
    }

    public async Task<ServiceResult> DeleteProductAsync(int productId, int userId)
    {
        var product = await _productRepository.GetByIdWithVariantsAndAttributesAsync(productId, true);
        if (product == null)
        {
            return ServiceResult.Fail("Product not found.");
        }

        var categoryGroupId = product.CategoryGroupId;

        product.IsDeleted = true;
        product.DeletedAt = DateTime.UtcNow;
        _productRepository.Update(product);

        var media = await _mediaService.GetEntityMediaAsync("Product", productId);
        foreach (var m in media)
        {
            await _mediaService.DeleteMediaAsync(m.Id);
        }

        await _unitOfWork.SaveChangesAsync();
        await _auditService.LogProductEventAsync(productId, "DeleteProduct", $"Product '{product.Name}' deleted.", userId);

        await InvalidateProductCachesAsync(productId, categoryGroupId);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RestoreProductAsync(int productId, int userId)
    {
        var product = await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null)
        {
            return ServiceResult.Fail("Product not found.");
        }

        product.IsDeleted = false;
        product.DeletedAt = null;
        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogProductEventAsync(productId, "RestoreProduct", $"Product '{product.Name}' restored.", userId);
        await InvalidateProductCachesAsync(productId, product.CategoryGroupId);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<List<AttributeTypeWithValuesDto>>> GetAllAttributesWithValuesAsync()
    {
        var attributes = await _productRepository.GetAllAttributeTypesWithValuesAsync();
        var dtos = _mapper.Map<List<AttributeTypeWithValuesDto>>(attributes);
        return ServiceResult<List<AttributeTypeWithValuesDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<object>> GetProductStatisticsAsync()
    {
        var stats = await _productRepository.GetProductStatisticsAsync();
        return ServiceResult<object>.Ok(stats);
    }

    public async Task<ServiceResult<IEnumerable<object>>> GetLowStockProductsAsync(int threshold)
    {
        var products = await _productRepository.GetLowStockProductsAsync(threshold);
        return ServiceResult<IEnumerable<object>>.Ok(products);
    }

    private async Task<AdminProductViewDto> MapToAdminViewDto(Product product)
    {
        var dto = _mapper.Map<AdminProductViewDto>(product);
        dto.IconUrl = await _mediaService.GetPrimaryImageUrlAsync("Product", product.Id);
        dto.Images = await _mediaService.GetEntityMediaAsync("Product", product.Id);
        dto.Variants = await MapVariantsToDto(product.Variants, product.Id);
        return dto;
    }

    private async Task<List<ProductVariantResponseDto>> MapVariantsToDto(ICollection<ProductVariant> variants, int productId)
    {
        var dtos = new List<ProductVariantResponseDto>();
        foreach (var v in variants)
        {
            var dto = _mapper.Map<ProductVariantResponseDto>(v);
            dto.Images = await _mediaService.GetEntityMediaAsync("ProductVariant", v.Id);

            dto.ShippingMultiplier = v.ShippingMultiplier;
            dto.EnabledShippingMethodIds = v.ProductVariantShippingMethods
                .Where(sm => sm.IsActive)
                .Select(sm => sm.ShippingMethodId)
                .ToList();

            dto.Attributes = v.VariantAttributes
                .Where(va => va.AttributeValue?.AttributeType != null)
                .ToDictionary(
                    va => va.AttributeValue!.AttributeType.Name.ToLowerInvariant(),
                    va => new AttributeValueDto(
                        va.AttributeValue!.Id,
                        va.AttributeValue.AttributeType.Name,
                        va.AttributeValue.AttributeType.DisplayName,
                        va.AttributeValue.Value,
                        va.AttributeValue.DisplayValue ?? va.AttributeValue.Value,
                        va.AttributeValue.HexCode
                    )
                );

            dtos.Add(dto);
        }
        return dtos;
    }

    private async Task InvalidateProductCachesAsync(int productId, int? categoryGroupId = null)
    {
        await _cacheService.ClearAsync($"{ProductCachePrefix}{productId}");
        await _cacheService.ClearByTagAsync($"{ProductTagPrefix}{productId}");
        await _cacheService.ClearByPrefixAsync(ProductListCachePrefix);

        if (categoryGroupId.HasValue)
        {
            await InvalidateCategoryGroupCacheAsync(categoryGroupId.Value);
        }

        _logger.LogDebug("Product caches invalidated for product {ProductId}", productId);
    }

    private async Task InvalidateCategoryGroupCacheAsync(int categoryGroupId)
    {
        await _cacheService.ClearAsync($"categorygroup:{categoryGroupId}");
        await _cacheService.ClearByPrefixAsync("categories:");
    }
}