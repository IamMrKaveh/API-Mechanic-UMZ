using Application.Audit.Contracts;
using Application.Security.Contracts;

namespace Application.Product.Features.Commands.UpdateVariant;

public class UpdateVariantHandler : IRequestHandler<UpdateVariantCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository;
    private readonly IAttributeRepository _attributeRepository;
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _auditService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateVariantHandler(
        IProductRepository productRepository,
        IAttributeRepository attributeRepository,
        IShippingMethodRepository shippingMethodRepository,
        IUnitOfWork unitOfWork,
        IAuditService auditService,
        ICurrentUserService currentUserService)
    {
        _productRepository = productRepository;
        _attributeRepository = attributeRepository;
        _shippingMethodRepository = shippingMethodRepository;
        _unitOfWork = unitOfWork;
        _auditService = auditService;
        _currentUserService = currentUserService;
    }

    public async Task<ServiceResult> Handle(UpdateVariantCommand request, CancellationToken ct)
    {
        var product = await _productRepository.GetByIdWithVariantsAsync(request.ProductId, ct);
        if (product == null)
            return ServiceResult.Failure("Product not found.");

        var variant = product.FindVariant(request.VariantId);
        if (variant == null)
            return ServiceResult.Failure("Variant not found.");

        // 1. Update variant details (sku, shipping multiplier) via aggregate
        product.UpdateVariantDetails(request.VariantId, request.Sku, request.ShippingMultiplier);

        // 2. Update prices via aggregate
        product.ChangeVariantPrices(
            request.VariantId,
            request.PurchasePrice,
            request.SellingPrice,
            request.OriginalPrice);

        // 3. Update stock via aggregate
        if (!request.IsUnlimited)
        {
            var stockDiff = request.Stock - variant.StockQuantity;
            if (stockDiff > 0)
                product.IncreaseStock(request.VariantId, stockDiff);
            else if (stockDiff < 0)
                product.DecreaseStock(request.VariantId, Math.Abs(stockDiff));
        }

        product.SetVariantUnlimited(request.VariantId, request.IsUnlimited);

        // 4. Update attributes via aggregate
        var attributeValues = request.AttributeValueIds.Any()
            ? await _attributeRepository.GetValuesByIdsAsync(request.AttributeValueIds, ct)
            : new List<AttributeValue>();

        if (request.AttributeValueIds.Any())
        {
            var missingIds = request.AttributeValueIds.Except(attributeValues.Select(av => av.Id)).ToList();
            if (missingIds.Any())
                return ServiceResult.Failure($"Invalid attribute values: {string.Join(", ", missingIds)}");
        }

        product.SetVariantAttributes(request.VariantId, attributeValues);

        // 5. Update shipping methods via aggregate
        if (request.EnabledShippingMethodIds != null)
        {
            var shippingMethods = request.EnabledShippingMethodIds.Any()
                ? await _shippingMethodRepository.GetByIdsAsync(request.EnabledShippingMethodIds, ct)
                : new List<ShippingMethod>();

            product.SetVariantShippingMethods(request.VariantId, shippingMethods);
        }

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogProductEventAsync(
            product.Id, "UpdateVariant",
            $"Variant {request.VariantId} updated on product '{product.Name}'.",
            _currentUserService.UserId);

        return ServiceResult.Success();
    }
}