using Domain.Attribute.Entities;

namespace Application.Product.Features.Commands.UpdateProduct;

public class UpdateProductHandler : IRequestHandler<UpdateProductCommand, ServiceResult<AdminProductViewDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IAttributeRepository _attributeRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IMediaService _mediaService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IProductQueryService _productQueryService;
    private readonly ILogger<UpdateProductHandler> _logger;

    public UpdateProductHandler(
        IProductRepository productRepository,
        IAttributeRepository attributeRepository,
        IInventoryService inventoryService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IHtmlSanitizer htmlSanitizer,
        IMediaService mediaService,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        IProductQueryService productQueryService,
        ILogger<UpdateProductHandler> logger)
    {
        _productRepository = productRepository;
        _attributeRepository = attributeRepository;
        _inventoryService = inventoryService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _htmlSanitizer = htmlSanitizer;
        _mediaService = mediaService;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _productQueryService = productQueryService;
        _logger = logger;
    }

    public async Task<ServiceResult<AdminProductViewDto>> Handle(
        UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdWithVariantsAsync(request.Id, cancellationToken);
        if (product == null)
            return ServiceResult<AdminProductViewDto>.Failure("Product not found.");

        // Validation
        if (!string.IsNullOrEmpty(request.Sku)
            && await _productRepository.ExistsBySkuAsync(request.Sku, request.Id, cancellationToken))
            return ServiceResult<AdminProductViewDto>.Failure("Product SKU already exists.");

        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Concurrency Check
                if (!string.IsNullOrEmpty(request.RowVersion))
                    _productRepository.SetOriginalRowVersion(product, Convert.FromBase64String(request.RowVersion));

                // Update Root
                product.UpdateDetails(
                    _htmlSanitizer.Sanitize(request.Name),
                    _htmlSanitizer.Sanitize(request.Description ?? string.Empty),
                    request.Sku,
                    request.CategoryGroupId,
                    request.IsActive);

                // Update Variants
                var variantDtos = ParseVariants(request.VariantsJson);
                await SynchronizeVariantsAsync(product, variantDtos, cancellationToken);

                // Media Handling
                if (request.DeletedMediaIds != null)
                    foreach (var id in request.DeletedMediaIds)
                        await _mediaService.DeleteMediaAsync(id);

                if (request.Images != null)
                    foreach (var img in request.Images)
                        await _mediaService.AttachFileToEntityAsync(
                            img.Content, img.FileName, img.ContentType, img.Length,
                            "Product", product.Id, false, product.Name, false);

                _productRepository.Update(product);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _auditService.LogProductEventAsync(
                    request.Id, "UpdateProduct", "Product updated", _currentUserService.UserId);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Return result via query service
                var detail = await _productQueryService.GetAdminProductDetailAsync(product.Id, cancellationToken);
                var result = _mapper.Map<AdminProductViewDto>(detail);
                return ServiceResult<AdminProductViewDto>.Success(result);
            }
            catch (DbUpdateConcurrencyException)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ServiceResult<AdminProductViewDto>.Failure(
                    "Concurrency Conflict: The record was modified by another user.");
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error updating product {Id}", request.Id);
                return ServiceResult<AdminProductViewDto>.Failure("Error updating product.");
            }
        }, cancellationToken);
    }

    private List<CreateProductVariantDto> ParseVariants(string json)
    {
        return JsonSerializer.Deserialize<List<CreateProductVariantDto>>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            }) ?? [];
    }

    private async Task SynchronizeVariantsAsync(
        Domain.Product.Product product,
        List<CreateProductVariantDto> variantDtos,
        CancellationToken ct)
    {
        // 1. Delete removed variants
        var incomingIds = variantDtos.Where(v => v.Id is > 0).Select(v => v.Id!.Value).ToList();
        var existingVariants = product.Variants.ToList();
        var variantsToDelete = existingVariants.Where(v => !incomingIds.Contains(v.Id)).ToList();

        foreach (var v in variantsToDelete)
        {
            product.RemoveVariant(v.Id, _currentUserService.UserId);
        }

        // 2. Update/Create
        foreach (var dto in variantDtos)
        {
            // Load attribute values
            var attributeValues = dto.AttributeValueIds.Any()
                ? await _attributeRepository.GetValuesByIdsAsync(dto.AttributeValueIds, ct)
                : new List<AttributeValue>();

            if (dto.Id is > 0)
            {
                // Update existing variant via aggregate methods
                var existing = product.FindVariant(dto.Id.Value);
                if (existing != null)
                {
                    product.UpdateVariantDetails(dto.Id.Value, dto.Sku, dto.ShippingMultiplier);
                    product.ChangeVariantPrices(dto.Id.Value, dto.PurchasePrice, dto.SellingPrice, dto.OriginalPrice);

                    // Stock adjustment via inventory service
                    int stockDiff = dto.Stock - existing.StockQuantity;
                    if (stockDiff != 0 && !existing.IsUnlimited)
                    {
                        await _inventoryService.AdjustStockAsync(
                            dto.Id.Value,
                            stockDiff,
                            Convert.ToInt32(_currentUserService.UserId),
                            "Admin update adjustment",
                            ct);
                    }

                    product.SetVariantUnlimited(dto.Id.Value, dto.IsUnlimited);
                    product.SetVariantAttributes(dto.Id.Value, attributeValues);
                }
            }
            else
            {
                // Create New via aggregate
                var newVariant = product.AddVariant(
                    dto.Sku,
                    dto.PurchasePrice,
                    dto.SellingPrice,
                    dto.OriginalPrice,
                    dto.Stock,
                    dto.IsUnlimited,
                    dto.ShippingMultiplier,
                    attributeValues);

                // Log initial stock via inventory service
                if (dto.Stock > 0 && !dto.IsUnlimited)
                {
                    await _inventoryService.LogTransactionAsync(
                        newVariant.Id,
                        "StockIn",
                        dto.Stock,
                        null,
                        _currentUserService.UserId,
                        "Admin update (new variant)",
                        null, null, false, ct);
                }
            }
        }
    }
}