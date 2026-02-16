using Domain.Attribute.Entities;

namespace Application.Product.Features.Commands.AddVariant;

public class AddVariantHandler : IRequestHandler<AddVariantCommand, ServiceResult<ProductVariantViewDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IAttributeRepository _attributeRepository;
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductQueryService _productQueryService;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public AddVariantHandler(
        IProductRepository productRepository,
        IAttributeRepository attributeRepository,
        IShippingMethodRepository shippingMethodRepository,
        IUnitOfWork unitOfWork,
        IProductQueryService productQueryService,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _productRepository = productRepository;
        _attributeRepository = attributeRepository;
        _shippingMethodRepository = shippingMethodRepository;
        _unitOfWork = unitOfWork;
        _productQueryService = productQueryService;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResult<ProductVariantViewDto>> Handle(AddVariantCommand request, CancellationToken ct)
    {
        var product = await _productRepository.GetByIdWithVariantsAsync(request.ProductId, ct);
        if (product == null)
            return ServiceResult<ProductVariantViewDto>.Failure("Product not found.");

        // Load attribute values
        var attributeValues = request.AttributeValueIds.Any()
            ? await _attributeRepository.GetValuesByIdsAsync(request.AttributeValueIds, ct)
            : new List<AttributeValue>();

        if (request.AttributeValueIds.Any())
        {
            var missingIds = request.AttributeValueIds.Except(attributeValues.Select(av => av.Id)).ToList();
            if (missingIds.Any())
                return ServiceResult<ProductVariantViewDto>.Failure($"Invalid attribute values: {string.Join(", ", missingIds)}");
        }

        // Domain operation — all validation happens inside Product aggregate
        var variant = product.AddVariant(
            request.Sku,
            request.PurchasePrice,
            request.SellingPrice,
            request.OriginalPrice,
            request.Stock,
            request.IsUnlimited,
            request.ShippingMultiplier,
            attributeValues);

        // Add shipping methods via aggregate
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

        await _auditService.LogProductEventAsync(
            product.Id, "AddVariant", $"Variant added to product '{product.Name}'.", _currentUserService.UserId);

        // Return via query service
        var variants = await _productQueryService.GetProductVariantsAsync(product.Id, false, ct);
        var result = variants.FirstOrDefault(v => v.Id == variant.Id);

        return ServiceResult<ProductVariantViewDto>.Success(result!);
    }
}