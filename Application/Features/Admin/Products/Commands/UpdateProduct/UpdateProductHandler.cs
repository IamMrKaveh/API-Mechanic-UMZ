namespace Application.Features.Admin.Products.Commands.UpdateProduct;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, ServiceResult<AdminProductViewDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper; 
    private readonly IHtmlSanitizer _htmlSanitizer; 
    private readonly IMediaService _mediaService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IInventoryService _inventoryService;
    private readonly LedkaContext _context;
    private readonly ILogger<UpdateProductHandler> _logger;

    public UpdateProductHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IHtmlSanitizer htmlSanitizer,
        IMediaService mediaService,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        IInventoryService inventoryService,
        LedkaContext context,
        ILogger<UpdateProductHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _htmlSanitizer = htmlSanitizer;
        _mediaService = mediaService;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _inventoryService = inventoryService;
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResult<AdminProductViewDto>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var product = await _productRepository.GetByIdWithVariantsAndAttributesAsync(request.Id, true);
                if (product == null) return ServiceResult<AdminProductViewDto>.Fail("Product not found.");

                if (!string.IsNullOrEmpty(request.Sku) && await _productRepository.ProductSkuExistsAsync(request.Sku, request.Id))
                    return ServiceResult<AdminProductViewDto>.Fail("Product SKU already exists.");

                if (!string.IsNullOrEmpty(request.RowVersion))
                    _productRepository.SetOriginalRowVersion(product, Convert.FromBase64String(request.RowVersion));

                // Handle Deleted Media
                if (request.DeletedMediaIds != null && request.DeletedMediaIds.Any())
                {
                    foreach (var mediaId in request.DeletedMediaIds) await _mediaService.DeleteMediaAsync(mediaId);
                }

                // Update Basic Info
                product.Name = _htmlSanitizer.Sanitize(request.Name);
                product.Description = _htmlSanitizer.Sanitize(request.Description ?? string.Empty);
                product.CategoryGroupId = request.CategoryGroupId;
                product.IsActive = request.IsActive;
                product.Sku = request.Sku;
                product.UpdatedAt = DateTime.UtcNow;

                // Handle Variants
                var variantDtos = JsonSerializer.Deserialize<List<CreateProductVariantDto>>(
                    request.VariantsJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        NumberHandling = JsonNumberHandling.AllowReadingFromString
                    }) ?? [];

                await UpdateVariantsAsync(product, variantDtos, _currentUserService.UserId ?? 0);

                product.RecalculateAggregates();

                // Handle New Images
                if (request.Images != null)
                {
                    foreach (var image in request.Images)
                    {
                        await _mediaService.AttachFileToEntityAsync(
                            image.Content,
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

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _auditService.LogProductEventAsync(request.Id, "UpdateProduct", $"Product updated.", _currentUserService.UserId);
                await transaction.CommitAsync();

                var loadedProduct = await _productRepository.GetByIdWithVariantsAndAttributesAsync(product.Id, true);
                var resultDto = _mapper.Map<AdminProductViewDto>(loadedProduct ?? product);
                resultDto.Images = await _mediaService.GetEntityMediaAsync("Product", product.Id);
                resultDto.IconUrl = await _mediaService.GetPrimaryImageUrlAsync("Product", product.Id);

                return ServiceResult<AdminProductViewDto>.Ok(resultDto);
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return ServiceResult<AdminProductViewDto>.Fail("Concurrency Conflict");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating product {ProductId}", request.Id);
                return ServiceResult<AdminProductViewDto>.Fail("Update failed.");
            }
        });
    }

    private async Task UpdateVariantsAsync(Product product, List<CreateProductVariantDto> variantDtos, int userId)
    {
        var existingVariantIds = product.Variants.Select(v => v.Id).ToList();
        var updatedVariantIds = variantDtos.Where(v => v.Id > 0).Select(v => v.Id!.Value).ToList();

        // Soft delete removed variants
        foreach (var variant in product.Variants.Where(v => !updatedVariantIds.Contains(v.Id)))
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

                int diff = dto.Stock - variant.StockQuantity;

                _mapper.Map(dto, variant);

                // Update Shipping Methods
                var existingMethods = variant.ProductVariantShippingMethods.ToList();
                var toRemove = existingMethods.Where(x => !dto.EnabledShippingMethodIds.Contains(x.ShippingMethodId)).ToList();
                foreach (var item in toRemove) _context.ProductVariantShippingMethods.Remove(item);

                var existingIds = existingMethods.Select(x => x.ShippingMethodId).ToList();
                var toAdd = dto.EnabledShippingMethodIds.Where(id => !existingIds.Contains(id)).ToList();
                foreach (var methodId in toAdd)
                {
                    variant.ProductVariantShippingMethods.Add(new ProductVariantShippingMethod { ShippingMethodId = methodId, IsActive = true });
                }

                if (diff != 0 && !variant.IsUnlimited)
                {
                    await _inventoryService.LogTransactionAsync(variant.Id, "Adjustment", diff, null, userId, "Admin update", null, null, false);
                }
            }
            else
            {
                variant = _mapper.Map<ProductVariant>(dto);
                variant.Product = product; // Ensure association

                foreach (var methodId in dto.EnabledShippingMethodIds)
                {
                    variant.ProductVariantShippingMethods.Add(new ProductVariantShippingMethod { ShippingMethodId = methodId, IsActive = true });
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

            // Update Attributes
            variant.VariantAttributes.Clear();
            var attributeValues = await _context.AttributeValues.Where(av => dto.AttributeValueIds.Contains(av.Id)).ToListAsync();
            foreach (var attrValue in attributeValues)
            {
                variant.VariantAttributes.Add(new ProductVariantAttribute { AttributeValue = attrValue });
            }
        }
    }
}