using Domain.Attribute.Entities;
using Domain.Attribute.Interfaces;
using Domain.Product.Interfaces;
using Domain.Product.ValueObjects;
using Domain.Shipping.Interfaces;
using Domain.User.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Application.Variant.Features.Commands.UpdateVariant;

public class UpdateVariantHandler(
    IProductRepository productRepository,
    IAttributeRepository attributeRepository,
    IShippingRepository shippingMethodRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<UpdateVariantCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        UpdateVariantCommand request,
        CancellationToken ct)
    {
        var productId = ProductId.From(request.ProductId);
        var userId = UserId.From(request.UserId);
        var variantId = VariantId.From(request.VariantId);

        var product = await productRepository.GetByIdAsync(productId, ct);
        if (product is null)
            return ServiceResult.NotFound("Product not found.");

        var variant = product.FindVariant(variantId);
        if (variant == null)
            return ServiceResult.NotFound("Variant not found.");

        product.UpdateVariantDetails(variantId, request.Sku, request.ShippingMultiplier);

        product.ChangeVariantPrices(
            variantId,
            request.PurchasePrice,
            request.SellingPrice,
            request.OriginalPrice);

        if (!request.IsUnlimited)
        {
            var stockDiff = request.Stock - variant.StockQuantity;
            if (stockDiff > 0)
                product.IncreaseStock(variantId, stockDiff);
            else if (stockDiff < 0)
                product.DecreaseStock(variantId, Math.Abs(stockDiff));
        }

        product.SetVariantUnlimited(variantId, request.IsUnlimited);

        var attributeValues = request.AttributeValueIds.Count != 0
            ? await attributeRepository.GetAttributeValuesByIdsAsync(request.AttributeValueIds, ct)
            : new List<AttributeValue>();

        if (request.AttributeValueIds.Count != 0)
        {
            var missingIds = request.AttributeValueIds.Except(attributeValues.Select(av => av.Id)).ToList();
            if (missingIds.Any())
                return ServiceResult.Failure($"Invalid attribute values: {string.Join(", ", missingIds)}");
        }

        product.SetVariantAttributes(variantId, attributeValues);

        if (request.EnabledShippingMethodIds != null)
        {
            var shippings = request.EnabledShippingMethodIds.Count != 0
                ? await shippingMethodRepository.GetByIdsAsync(request.EnabledShippingMethodIds, ct)
                : new List<Domain.Shipping.Aggregates.Shipping>();

            product.SetVariantShippingMethods(variantId, shippings);
        }

        productRepository.Update(product);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogProductEventAsync(
            product.Id, "UpdateVariant",
            $"Variant {variant} updated on product '{product.Name}'.",
            userId);

        return ServiceResult.Success();
    }
}