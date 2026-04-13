using Domain.Attribute.Interfaces;
using Domain.Attribute.ValueObjects;
using Domain.Inventory.Interfaces;
using Domain.Product.ValueObjects;
using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;
using Domain.User.ValueObjects;
using Domain.Variant.Interfaces;
using Domain.Variant.ValueObjects;

namespace Application.Variant.Features.Commands.UpdateVariant;

public class UpdateVariantHandler(
    IVariantRepository variantRepository,
    IInventoryRepository inventoryRepository,
    IAttributeRepository attributeRepository,
    IShippingRepository shippingRepository,
    IUnitOfWork unitOfWork,
    IAuditService auditService) : IRequestHandler<UpdateVariantCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        UpdateVariantCommand request,
        CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var userId = UserId.From(request.UserId);
        var productId = ProductId.From(request.ProductId);

        var variant = await variantRepository.GetByIdAsync(variantId, ct);
        if (variant is null)
            return ServiceResult.NotFound("واریانت یافت نشد.");

        if (variant.ProductId != productId)
            return ServiceResult.Validation("واریانت متعلق به این محصول نیست.");

        if (request.Sku is not null)
        {
            var newSku = Sku.Create(request.Sku);
            var skuExists = await variantRepository.ExistsBySkuAsync(newSku, variantId, ct);
            if (skuExists)
                return ServiceResult.Conflict("این SKU قبلاً استفاده شده است.");
            variant.ChangeSku(newSku);
        }

        var price = Money.FromDecimal(request.SellingPrice);
        var compareAtPrice = request.OriginalPrice > request.SellingPrice
            ? Money.FromDecimal(request.OriginalPrice)
            : null;
        variant.ChangePrice(price, compareAtPrice);

        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is not null)
        {
            if (request.IsUnlimited)
            {
                inventory.SetUnlimited();
            }
            else
            {
                var currentStock = inventory.StockQuantity;
                var stockDiff = request.Stock - (int)currentStock;
                if (stockDiff > 0)
                    inventory.IncreaseStock(stockDiff, "به‌روزرسانی موجودی", userId);
                else if (stockDiff < 0)
                    inventory.DecreaseStock(Math.Abs(stockDiff), "به‌روزرسانی موجودی", userId);
            }
            inventoryRepository.Update(inventory);
        }

        if (request.AttributeValueIds is not null)
        {
            var attributeValueIds = request.AttributeValueIds.Select(AttributeValueId.From);
            var attributeValues = request.AttributeValueIds.Count != 0
                ? await attributeRepository.GetAttributeValuesByIdsAsync(attributeValueIds, ct)
                : [];

            if (request.AttributeValueIds.Count != 0)
            {
                var missingIds = request.AttributeValueIds
                    .Except(attributeValues.Select(av => av.Id.Value))
                    .ToList();
                if (missingIds.Count != 0)
                    return ServiceResult.Failure($"شناسه‌های ویژگی نامعتبر: {string.Join(", ", missingIds)}");
            }

            var assignments = attributeValues.Select(av =>
                AttributeAssignment.Create(
                    av.AttributeTypeId,
                    av.Id,
                    av.Value));
            variant.SetAttributes(assignments);
        }

        if (request.EnabledShippingIds is not null)
        {
            var newShippingIds = request.EnabledShippingIds.Select(ShippingId.From);

            var shippings = request.EnabledShippingIds.Count != 0
                ? await shippingRepository.GetByIdsAsync(newShippingIds, ct)
                : [];

            var shippingAssignments = shippings.Select(s =>
                new ShippingAssignment(s.Id, 0, 0, 0, 0));
            variant.SetShippingMethods(shippingAssignments);
        }

        variantRepository.Update(variant);
        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogProductEventAsync(
            productId,
            "UpdateVariant",
            $"واریانت {variantId.Value} به‌روزرسانی شد.",
            userId);

        return ServiceResult.Success();
    }
}