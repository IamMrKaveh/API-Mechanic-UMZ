namespace Application.Variant.Features.Commands.AddVariant;

public class AddVariantHandler : IRequestHandler<AddVariantCommand, ServiceResult<ProductVariantViewDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IAttributeRepository _attributeRepository;
    private readonly IShippingRepository _shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductQueryService _productQueryService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<AddVariantHandler> _logger;

    public AddVariantHandler(
        IProductRepository productRepository,
        IAttributeRepository attributeRepository,
        IShippingRepository shippingMethodRepository,
        IUnitOfWork unitOfWork,
        IProductQueryService productQueryService,
        IAuditService auditService,
        ICurrentUserService currentUserService,
        ILogger<AddVariantHandler> logger)
    {
        _productRepository = productRepository;
        _attributeRepository = attributeRepository;
        _shippingMethodRepository = shippingMethodRepository;
        _unitOfWork = unitOfWork;
        _productQueryService = productQueryService;
        _auditService = auditService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<ServiceResult<ProductVariantViewDto>> Handle(AddVariantCommand request, CancellationToken ct)
    {
        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
            try
            {
                var product = await _productRepository.GetByIdWithVariantsAsync(request.ProductId, ct);
                if (product == null)
                    return ServiceResult<ProductVariantViewDto>.Failure("Product not found.", 404);

                // Batch load attributes to prevent N+1
                var attributeValues = request.AttributeValueIds.Any()
                    ? await _attributeRepository.GetValuesByIdsAsync(request.AttributeValueIds, ct)
                    : new List<AttributeValue>();

                if (request.AttributeValueIds.Any())
                {
                    var missingIds = request.AttributeValueIds.Except(attributeValues.Select(av => av.Id)).ToList();
                    if (missingIds.Any())
                        return ServiceResult<ProductVariantViewDto>.Failure($"Invalid attribute values: {string.Join(", ", missingIds)}");
                }

                // Domain Rule Executions inside Aggregate
                var variant = product.AddVariant(
                    request.Sku,
                    request.PurchasePrice,
                    request.SellingPrice,
                    request.OriginalPrice,
                    request.Stock,
                    request.IsUnlimited,
                    request.ShippingMultiplier,
                    attributeValues);

                if (request.EnabledShippingMethodIds != null && request.EnabledShippingMethodIds.Any())
                {
                    var shippingMethods = await _shippingMethodRepository.GetByIdsAsync(request.EnabledShippingMethodIds, ct);
                    foreach (var sm in shippingMethods)
                    {
                        product.AddVariantShippingMethod(variant.Id, sm);
                    }
                }

                _productRepository.Update(product);
                await _unitOfWork.SaveChangesAsync(ct);
                await _unitOfWork.CommitTransactionAsync(ct);

                await _auditService.LogProductEventAsync(
                    product.Id, "AddVariant", $"Variant added to product '{product.Name}'. SKU: {variant.Sku}", _currentUserService.UserId);

                // Query Side Return
                var variants = await _productQueryService.GetProductVariantsAsync(product.Id, false, ct);
                var result = variants.FirstOrDefault(v => v.Id == variant.Id);

                return ServiceResult<ProductVariantViewDto>.Success(result!);
            }
            catch (DomainException ex)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                return ServiceResult<ProductVariantViewDto>.Failure(ex.Message, 400);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                _logger.LogError(ex, "Error occurred while adding variant to product {ProductId}", request.ProductId);
                return ServiceResult<ProductVariantViewDto>.Failure("An error occurred while adding the variant.", 500);
            }
        }, ct);
    }
}