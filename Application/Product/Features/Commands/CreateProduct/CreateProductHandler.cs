namespace Application.Product.Features.Commands.CreateProduct;

public class CreateProductHandler
    : IRequestHandler<CreateProductCommand, ServiceResult<AdminProductDetailDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IAttributeRepository _attributeRepository;
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IMediaService _mediaService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProductQueryService _productQueryService;
    private readonly ILogger<CreateProductHandler> _logger;

    public CreateProductHandler(
        IProductRepository productRepository,
        IAttributeRepository attributeRepository,
        IShippingMethodRepository shippingMethodRepository,
        IUnitOfWork unitOfWork,
        IHtmlSanitizer htmlSanitizer,
        IMediaService mediaService,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        IProductQueryService productQueryService,
        ILogger<CreateProductHandler> logger)
    {
        _productRepository = productRepository;
        _attributeRepository = attributeRepository;
        _shippingMethodRepository = shippingMethodRepository;
        _unitOfWork = unitOfWork;
        _htmlSanitizer = htmlSanitizer;
        _mediaService = mediaService;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _productQueryService = productQueryService;
        _logger = logger;
    }

    public async Task<ServiceResult<AdminProductDetailDto>> Handle(
        CreateProductCommand request,
        CancellationToken ct)
    {
        // 1. SKU validation
        if (!string.IsNullOrEmpty(request.Sku) &&
            await _productRepository.ExistsBySkuAsync(request.Sku, ct: ct))
        {
            return ServiceResult<AdminProductDetailDto>
                .Failure("Product SKU already exists.");
        }

        // 2. Attribute validation
        var allAttrValueIds = request.Variants
            .SelectMany(v => v.AttributeValueIds)
            .Distinct()
            .ToList();

        var attributeValues = allAttrValueIds.Any()
            ? await _attributeRepository.GetValuesByIdsAsync(allAttrValueIds, ct)
            : new List<AttributeValue>();

        if (allAttrValueIds.Any())
        {
            var missingIds = allAttrValueIds
                .Except(attributeValues.Select(x => x.Id))
                .ToList();

            if (missingIds.Any())
            {
                return ServiceResult<AdminProductDetailDto>
                    .Failure($"Invalid attribute values: {string.Join(", ", missingIds)}");
            }
        }

        // 3. Shipping methods
        var shippingMethodIds = request.Variants
            .Where(v => v.EnabledShippingMethodIds != null)
            .SelectMany(v => v.EnabledShippingMethodIds!)
            .Distinct()
            .ToList();

        var shippingMethods = shippingMethodIds.Any()
            ? await _shippingMethodRepository.GetByIdsAsync(shippingMethodIds, ct)
            : new List<ShippingMethod>();

        Domain.Product.Product product = null!;

        try
        {
            // 4. ONLY database logic inside execution strategy + transaction
            await _unitOfWork.ExecuteStrategyAsync(async () =>
            {
                await _unitOfWork.BeginTransactionAsync(ct);

                product = Domain.Product.Product.Create(
                    _htmlSanitizer.Sanitize(request.Name),
                    request.Description != null
                        ? _htmlSanitizer.Sanitize(request.Description)
                        : null,
                    request.Sku,
                    request.CategoryGroupId);

                if (!request.IsActive)
                    product.Deactivate();

                foreach (var variantInput in request.Variants)
                {
                    var variantAttrs = attributeValues
                        .Where(av => variantInput.AttributeValueIds.Contains(av.Id))
                        .ToList();

                    var variant = product.AddVariant(
                        variantInput.Sku,
                        variantInput.PurchasePrice,
                        variantInput.SellingPrice,
                        variantInput.OriginalPrice,
                        variantInput.Stock,
                        variantInput.IsUnlimited,
                        variantInput.ShippingMultiplier,
                        variantAttrs);

                    if (variantInput.EnabledShippingMethodIds != null)
                    {
                        foreach (var smId in variantInput.EnabledShippingMethodIds)
                        {
                            var sm = shippingMethods.FirstOrDefault(x => x.Id == smId);
                            if (sm != null)
                                product.AddVariantShippingMethod(variant.Id, sm);
                        }
                    }
                }

                await _productRepository.AddAsync(product, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                await _unitOfWork.CommitTransactionAsync(ct);
            }, ct);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);
            _logger.LogError(ex, "Error creating product");
            return ServiceResult<AdminProductDetailDto>
                .Failure("Failed to create product.");
        }

        // 5. SIDE EFFECTS (OUTSIDE TRANSACTION)

        if (request.Images != null && request.Images.Any())
        {
            for (int i = 0; i < request.Images.Count; i++)
            {
                var image = request.Images[i];
                bool isPrimary =
                    request.PrimaryImageIndex.HasValue
                        ? request.PrimaryImageIndex.Value == i
                        : i == 0;

                await _mediaService.AttachFileToEntityAsync(
                    image.Content,
                    image.FileName,
                    image.ContentType,
                    image.Length,
                    "Product",
                    product.Id,
                    isPrimary,
                    product.Name,
                    false,
                    ct);
            }
        }

        await _auditService.LogProductEventAsync(
            product.Id,
            "CreateProduct",
            $"Product '{product.Name}' created.",
            _currentUserService.UserId);

        var result =
            await _productQueryService.GetAdminProductDetailAsync(product.Id, ct);

        return ServiceResult<AdminProductDetailDto>.Success(result!);
    }
}