using Application.Common.Interfaces;
using Domain.Attribute.Entities;
using Domain.Attribute.Interfaces;
using Domain.Product.Interfaces;
using Domain.Shipping.Interfaces;

namespace Application.Variant.Features.Commands.UpdateVariant;

public class UpdateVariantHandler(
    IProductRepository productRepository,
    IAttributeRepository attributeRepository,
    IShippingRepository shippingMethodRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateVariantCommand, ServiceResult>
{
    private readonly IProductRepository _productRepository = productRepository;
    private readonly IAttributeRepository _attributeRepository = attributeRepository;
    private readonly IShippingRepository _shippingMethodRepository = shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly IAuditService _auditService = auditService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<ServiceResult> Handle(
        UpdateVariantCommand request,
        CancellationToken ct)
    {
        var product = await _productRepository.GetByIdWithVariantsAsync(request.ProductId, ct);
        if (product == null)
            return ServiceResult.NotFound("Product not found.");

        var variant = product.FindVariant(request.VariantId);
        if (variant == null)
            return ServiceResult.NotFound("Variant not found.");

        product.UpdateVariantDetails(request.VariantId, request.Sku, request.ShippingMultiplier);

        product.ChangeVariantPrices(
            request.VariantId,
            request.PurchasePrice,
            request.SellingPrice,
            request.OriginalPrice);

        if (!request.IsUnlimited)
        {
            var stockDiff = request.Stock - variant.StockQuantity;
            if (stockDiff > 0)
                product.IncreaseStock(request.VariantId, stockDiff);
            else if (stockDiff < 0)
                product.DecreaseStock(request.VariantId, Math.Abs(stockDiff));
        }

        product.SetVariantUnlimited(request.VariantId, request.IsUnlimited);

        var attributeValues = request.AttributeValueIds.Any()
            ? await _attributeRepository.GetAttributeValuesByIdsAsync(request.AttributeValueIds, ct)
            : new List<AttributeValue>();

        if (request.AttributeValueIds.Count != 0)
        {
            var missingIds = request.AttributeValueIds.Except(attributeValues.Select(av => av.Id)).ToList();
            if (missingIds.Any())
                return ServiceResult.Unexpected($"Invalid attribute values: {string.Join(", ", missingIds)}");
        }

        product.SetVariantAttributes(request.VariantId, attributeValues);

        if (request.EnabledShippingMethodIds != null)
        {
            var shippingMethods = request.EnabledShippingMethodIds.Any()
                ? await _shippingMethodRepository.GetByIdsAsync(request.EnabledShippingMethodIds, ct)
                : new List<Domain.Shipping.Aggregates.Shipping>();

            product.SetVariantShippingMethods(request.VariantId, shippingMethods);
        }

        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(ct);

        await _auditService.LogProductEventAsync(
            product.Id, "UpdateVariant",
            $"Variant {request.VariantId} updated on product '{product.Name}'.",
            _currentUserService.CurrentUser.UserId);

        return ServiceResult.Success();
    }
}