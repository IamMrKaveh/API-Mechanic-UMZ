namespace Application.Services;

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

    public AdminProductService(
        IProductRepository productRepository,
        IInventoryService inventoryService,
        IAuditService auditService,
        ILogger<AdminProductService> logger,
        IHtmlSanitizer htmlSanitizer,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IMediaService mediaService)
    {
        _productRepository = productRepository;
        _inventoryService = inventoryService;
        _auditService = auditService;
        _logger = logger;
        _htmlSanitizer = htmlSanitizer;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _mediaService = mediaService;
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
        var product = _mapper.Map<Product>(productDto);
        product.Description = _htmlSanitizer.Sanitize(productDto.Description ?? string.Empty);

        var variants = JsonSerializer
            .Deserialize<List<CreateProductVariantDto>>(
            productDto.VariantsJson,
            new JsonSerializerOptions 
            {
                PropertyNameCaseInsensitive = true 
            }) ?? [];

        if (!variants.Any())
        {
            return ServiceResult<AdminProductViewDto>.Fail("Product must have at least one variant.");
        }

        foreach (var variantDto in variants)
        {
            var variant = _mapper.Map<ProductVariant>(variantDto);
            variant.Product = product;
            var attributeValues = await _productRepository.GetAttributeValuesByIdsAsync(variantDto.AttributeValueIds);
            foreach (var attrValue in attributeValues)
            {
                variant.VariantAttributes.Add(new ProductVariantAttribute { AttributeValue = attrValue });
            }
            product.Variants.Add(variant);
        }

        product.RecalculateAggregates();

        await _productRepository.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        if (productDto.Images != null)
        {
            foreach (var image in productDto.Images)
            {
                await _mediaService.AttachFileToEntityAsync(image.OpenReadStream(), image.FileName, image.ContentType, image.Length, "Product", product.Id, false, product.Name);
            }
        }
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogProductEventAsync(product.Id, "CreateProduct", $"Product '{product.Name}' created.", userId);
        _logger.LogInformation("Product {ProductId} created by user {UserId}", product.Id, userId);

        var resultDto = await MapToAdminViewDto(product);
        return ServiceResult<AdminProductViewDto>.Ok(resultDto);
    }

    public async Task<ServiceResult> UpdateProductAsync(int productId, ProductDto productDto, int userId)
    {
        var product = await _productRepository.GetByIdWithVariantsAndAttributesAsync(productId, true);
        if (product == null)
        {
            return ServiceResult.Fail("Product not found.");
        }

        if (!string.IsNullOrEmpty(productDto.RowVersion))
        {
            _productRepository.SetOriginalRowVersion(product, Convert.FromBase64String(productDto.RowVersion));
        }

        _mapper.Map(productDto, product);
        product.Description = _htmlSanitizer.Sanitize(productDto.Description ?? string.Empty);

        var variantDtos = JsonSerializer.Deserialize<List<CreateProductVariantDto>>(productDto.VariantsJson) ?? [];
        _productRepository.UpdateVariants(product, variantDtos);
        product.RecalculateAggregates();

        if (productDto.Images != null)
        {
            foreach (var image in productDto.Images)
            {
                await _mediaService.AttachFileToEntityAsync(image.OpenReadStream(), image.FileName, image.ContentType, image.Length, "Product", product.Id, false, product.Name);
            }
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
            await _auditService.LogProductEventAsync(productId, "UpdateProduct", $"Product '{product.Name}' updated.", userId);
            return ServiceResult.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail("The record was modified by another user. Please refresh and try again.");
        }
    }

    public async Task<ServiceResult> AddStockAsync(int variantId, int quantity, int userId, string notes)
    {
        var variant = await _productRepository.GetVariantByIdAsync(variantId);
        if (variant == null)
        {
            return ServiceResult.Fail("Variant not found.");
        }

        await _inventoryService.LogTransactionAsync(variantId, "StockIn", quantity, null, userId, notes, null, variant.RowVersion);

        await _unitOfWork.SaveChangesAsync();
        await _auditService.LogInventoryEventAsync(variant.ProductId, "AddStock", $"Added {quantity} to stock for variant {variantId}.", userId);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RemoveStockAsync(int variantId, int quantity, int userId, string notes)
    {
        var variant = await _productRepository.GetVariantByIdAsync(variantId);
        if (variant == null)
        {
            return ServiceResult.Fail("Variant not found.");
        }

        await _inventoryService.LogTransactionAsync(variantId, "StockOut", -quantity, null, userId, notes, null, variant.RowVersion);

        await _unitOfWork.SaveChangesAsync();
        await _auditService.LogInventoryEventAsync(variant.ProductId, "RemoveStock", $"Removed {quantity} from stock for variant {variantId}.", userId);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetDiscountAsync(int variantId, decimal originalPrice, decimal discountedPrice, int userId)
    {
        var variant = await _productRepository.GetVariantByIdAsync(variantId);
        if (variant == null)
        {
            return ServiceResult.Fail("Variant not found.");
        }

        if (discountedPrice >= originalPrice)
        {
            return ServiceResult.Fail("Discounted price must be less than the original price.");
        }

        variant.OriginalPrice = originalPrice;
        variant.SellingPrice = discountedPrice;

        _productRepository.UpdateVariant(variant);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogProductEventAsync(variant.ProductId, "SetDiscount", $"Discount set for variant {variantId}.", userId);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RemoveDiscountAsync(int variantId, int userId)
    {
        var variant = await _productRepository.GetVariantByIdAsync(variantId);
        if (variant == null)
        {
            return ServiceResult.Fail("Variant not found.");
        }

        variant.SellingPrice = variant.OriginalPrice;

        _productRepository.UpdateVariant(variant);
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogProductEventAsync(variant.ProductId, "RemoveDiscount", $"Discount removed for variant {variantId}.", userId);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> BulkUpdatePricesAsync(Dictionary<int, decimal> priceUpdates, bool isPurchasePrice, int userId)
    {
        var variantIds = priceUpdates.Keys.ToList();
        var variants = await _productRepository.GetVariantsByIdsAsync(variantIds);

        foreach (var (variantId, newPrice) in priceUpdates)
        {
            if (variants.TryGetValue(variantId, out var variant))
            {
                if (isPurchasePrice)
                {
                    variant.PurchasePrice = newPrice;
                }
                else
                {
                    variant.SellingPrice = newPrice;
                }
                _productRepository.UpdateVariant(variant);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        await _auditService.LogSystemEventAsync("BulkPriceUpdate", $"Bulk price update performed by user {userId}.", userId);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteProductAsync(int productId, int userId)
    {
        var product = await _productRepository.GetByIdWithVariantsAndAttributesAsync(productId, true);
        if (product == null)
        {
            return ServiceResult.Fail("Product not found.");
        }

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
            if (!dto.Images.Any())
            {
                var productImages = await _mediaService.GetEntityMediaAsync("Product", productId);
                dto.Images = _mapper.Map<IEnumerable<MediaDto>>(productImages);
            }
            dtos.Add(dto);
        }
        return dtos;
    }
}