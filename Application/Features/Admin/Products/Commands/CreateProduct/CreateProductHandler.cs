namespace Application.Features.Admin.Products.Commands.CreateProduct;

public class CreateProductHandler : IRequestHandler<CreateProductCommand, ServiceResult<AdminProductViewDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IMediaService _mediaService;
    private readonly IStorageService _storageService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateProductHandler> _logger;

    public CreateProductHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IHtmlSanitizer htmlSanitizer,
        IMediaService mediaService,
        IStorageService storageService,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        ILogger<CreateProductHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _htmlSanitizer = htmlSanitizer;
        _mediaService = mediaService;
        _storageService = storageService;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ServiceResult<AdminProductViewDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.Sku) && await _productRepository.ProductSkuExistsAsync(request.Sku))
        {
            return ServiceResult<AdminProductViewDto>.Fail("Product SKU already exists.");
        }

        var variants = JsonSerializer.Deserialize<List<CreateProductVariantDto>>(
               request.VariantsJson,
               new JsonSerializerOptions
               {
                   PropertyNameCaseInsensitive = true,
                   NumberHandling = JsonNumberHandling.AllowReadingFromString
               }) ?? [];

        if (!variants.Any())
        {
            return ServiceResult<AdminProductViewDto>.Fail("Product must have at least one variant.");
        }

        // Validate Attributes Uniqueness
        var variantAttributeSets = variants
            .Select(v => new HashSet<int>(v.AttributeValueIds))
            .ToList();

        if (variantAttributeSets.Count != new HashSet<HashSet<int>>(variantAttributeSets, HashSet<int>.CreateSetComparer()).Count)
        {
            return ServiceResult<AdminProductViewDto>.Fail("Duplicate variant combinations detected.");
        }

        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            var uploadedFilePaths = new List<string>();

            try
            {
                var product = new Domain.Product.Product
                {
                    Name = _htmlSanitizer.Sanitize(request.Name),
                    Description = _htmlSanitizer.Sanitize(request.Description ?? string.Empty),
                    CategoryGroupId = request.CategoryGroupId,
                    IsActive = request.IsActive,
                    Sku = request.Sku,
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var variantDto in variants)
                {
                    var variant = _mapper.Map<ProductVariant>(variantDto);
                    variant.Product = product;

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
                            UserId = _currentUserService.UserId
                        });
                    }

                    product.Variants.Add(variant);
                }

                await _productRepository.AddAsync(product);
                product.RecalculateAggregates();
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Handle Images
                if (request.Images != null)
                {
                    for (int i = 0; i < request.Images.Count; i++)
                    {
                        var image = request.Images[i];
                        bool isPrimary = (request.PrimaryImageIndex.HasValue && request.PrimaryImageIndex.Value == i) || (!request.PrimaryImageIndex.HasValue && i == 0);

                        var media = await _mediaService.AttachFileToEntityAsync(
                            image.Content,
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
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                await _auditService.LogProductEventAsync(product.Id, "CreateProduct", $"Product '{product.Name}' created.", _currentUserService.UserId);

                await transaction.CommitAsync();

                var loadedProduct = await _productRepository.GetByIdWithVariantsAndAttributesAsync(product.Id, true);
                var resultDto = _mapper.Map<AdminProductViewDto>(loadedProduct ?? product);

                // Manually map images as they might not be fully loaded in mapper
                resultDto.Images = await _mediaService.GetEntityMediaAsync("Product", product.Id);
                resultDto.IconUrl = await _mediaService.GetPrimaryImageUrlAsync("Product", product.Id);

                return ServiceResult<AdminProductViewDto>.Ok(resultDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                foreach (var path in uploadedFilePaths) await _storageService.DeleteFileAsync(path);
                _logger.LogError(ex, "Error creating product");
                return ServiceResult<AdminProductViewDto>.Fail("Failed to create product due to an internal error.");
            }
        });
    }
}