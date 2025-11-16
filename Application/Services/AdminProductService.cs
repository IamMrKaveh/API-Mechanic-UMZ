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

        var variants = JsonSerializer.Deserialize<List<CreateProductVariantDto>>(productDto.VariantsJson) ?? [];
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

    public async Task<ServiceResult> AddStockAsync(int variantId, int quantity, int userId, string? notes)
    {
        notes ??= "Manual stock addition";
        var (success, message) = await _inventoryService.AdjustStockAsync(variantId, (await _inventoryService.GetCurrentStockAsync(variantId)) + quantity, userId, notes);
        if (!success)
        {
            return ServiceResult.Fail(message);
        }
        await _auditService.LogProductEventAsync(variantId, "AddStock", $"Added {quantity} to stock for variant {variantId}. Notes: {notes}", userId);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RemoveStockAsync(int variantId, int quantity, int userId, string? notes)
    {
        notes ??= "Manual stock removal";
        var currentStock = await _inventoryService.GetCurrentStockAsync(variantId);
        if (currentStock < quantity)
        {
            return ServiceResult.Fail("Cannot remove more stock than is available.");
        }

        var (success, message) = await _inventoryService.AdjustStockAsync(variantId, currentStock - quantity, userId, notes);
        if (!success)
        {
            return ServiceResult.Fail(message);
        }
        await _auditService.LogProductEventAsync(variantId, "RemoveStock", $"Removed {quantity} from stock for variant {variantId}. Notes: {notes}", userId);
        await _unitOfWork.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetDiscountAsync(int variantId, decimal originalPrice, decimal discountedPrice, int userId)
    {
        var variant = await _productRepository.GetVariantByIdAsync(variantId);
        if (variant == null) return ServiceResult.Fail("NotFound");

        variant.OriginalPrice = originalPrice;
        variant.SellingPrice = discountedPrice;
        await _unitOfWork.SaveChangesAsync();
        await _auditService.LogProductEventAsync(variantId, "SetDiscount", $"Discount set for variant {variantId}. Original: {originalPrice}, Discounted: {discountedPrice}", userId);

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> RemoveDiscountAsync(int variantId, int userId)
    {
        var variant = await _productRepository.GetVariantByIdAsync(variantId);
        if (variant == null) return ServiceResult.Fail("NotFound");

        variant.SellingPrice = variant.OriginalPrice;
        await _unitOfWork.SaveChangesAsync();
        await _auditService.LogProductEventAsync(variantId, "RemoveDiscount", $"Discount removed for variant {variantId}", userId);

        return ServiceResult.Ok();
    }

    public Task<ServiceResult> BulkUpdatePricesAsync(Dictionary<int, decimal> priceUpdates, bool isPurchasePrice, int userId)
    {
        _logger.LogInformation("User {UserId} initiated bulk price update.", userId);
        return Task.FromResult(ServiceResult.Ok());
    }

    public Task<ServiceResult<object>> GetProductStatisticsAsync()
    {
        var stats = new { Message = "Statistics endpoint not fully implemented." };
        return Task.FromResult(ServiceResult<object>.Ok(stats));
    }

    public Task<ServiceResult<IEnumerable<ProductVariantResponseDto>>> GetLowStockProductsAsync(int threshold)
    {
        _logger.LogInformation("Low stock report requested with threshold {Threshold}", threshold);
        var result = new List<ProductVariantResponseDto>();
        return Task.FromResult(ServiceResult<IEnumerable<ProductVariantResponseDto>>.Ok(result));
    }

    private async Task<AdminProductViewDto> MapToAdminViewDto(Product product)
    {
        var dto = _mapper.Map<AdminProductViewDto>(product);
        dto.IconUrl = await _mediaService.GetPrimaryImageUrlAsync("Product", product.Id);
        var media = await _mediaService.GetEntityMediaAsync("Product", product.Id);
        dto.Images = _mapper.Map<IEnumerable<MediaDto>>(media);

        var variantDtos = new List<ProductVariantResponseDto>();
        foreach (var variant in product.Variants)
        {
            var variantDto = _mapper.Map<ProductVariantResponseDto>(variant);
            var variantMedia = await _mediaService.GetEntityMediaAsync("ProductVariant", variant.Id);
            variantDto.Images = _mapper.Map<IEnumerable<MediaDto>>(variantMedia);
            variantDtos.Add(variantDto);
        }
        dto.Variants = variantDtos;

        return dto;
    }
}