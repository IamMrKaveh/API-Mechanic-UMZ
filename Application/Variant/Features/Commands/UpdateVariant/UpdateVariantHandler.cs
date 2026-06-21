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
    IAuditService auditService,
    ICurrentUserService currentUserService) : IRequestHandler<UpdateVariantCommand, ServiceResult>
{
    public async Task<ServiceResult> Handle(
        UpdateVariantCommand request,
        CancellationToken ct)
    {
        var variantId = VariantId.From(request.VariantId);
        var userId = UserId.From(currentUserService.UserId.Value);
        var productId = ProductId.From(request.ProductId);

        var variant = await variantRepository.GetForUpdateAsync(variantId, ct);
        if (variant is null)
            return ServiceResult.NotFound("واریانت یافت نشد.");

        if (variant.ProductId != productId)
            return ServiceResult.Validation("واریانت متعلق به این محصول نیست.");

        if (request.Sku is not null)
        {
            var newSku = Sku.Create(request.Sku);
            if (!variant.Sku.Equals(newSku))
            {
                var skuExists = await variantRepository.ExistsBySkuAsync(newSku, variantId, ct);
                if (skuExists)
                    return ServiceResult.Conflict("این SKU قبلاً استفاده شده است.");
                variant.ChangeSku(newSku);
            }
        }

        var price = Money.FromDecimal(request.SellingPrice);
        var compareAtPrice = request.OriginalPrice > request.SellingPrice
            ? Money.FromDecimal(request.OriginalPrice)
            : null;
        variant.ChangePrice(price, compareAtPrice);

        var inventory = await inventoryRepository.GetByVariantIdAsync(variantId, ct);
        if (inventory is null)
        {
            var initialStock = request.IsUnlimited ? 0 : Math.Max(0, request.Stock);
            inventory = Domain.Inventory.Aggregates.Inventory.Create(
                variantId,
                initialStock,
                request.IsUnlimited,
                lowStockThreshold: 5,
                createdBy: userId);
            await inventoryRepository.AddAsync(inventory, ct);
        }
        else
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

                var duplicateTypes = attributeValues
                    .GroupBy(av => av.AttributeTypeId)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToList();

                if (duplicateTypes.Count != 0)
                    return ServiceResult.Validation("برای هر نوع ویژگی فقط یک مقدار مجاز است.");

                var signature = attributeValues
                    .Select(av => av.Id.Value)
                    .OrderBy(id => id)
                    .ToList();

                var duplicateVariantExists = await variantRepository.ExistsByAttributeCombinationAsync(
                    productId,
                    signature,
                    excludeId: variantId,
                    ct);

                if (duplicateVariantExists)
                    return ServiceResult.Conflict("تنوع دیگری با همین ترکیب ویژگی‌ها از قبل وجود دارد.");
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
            variant.SetShippingMethods(
                request.ShippingMultiplier,
                shippingAssignments);
        }

        await unitOfWork.SaveChangesAsync(ct);

        await auditService.LogProductEventAsync(
            productId,
            "UpdateVariant",
            $"واریانت {variantId.Value} به‌روزرسانی شد.",
            userId);

        return ServiceResult.Success();
    }
}