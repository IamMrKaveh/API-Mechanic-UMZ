using Application.Attribute.Features.Shared;
using Application.Shipping.Features.Shared;
using Application.Variant.Contracts;
using Application.Variant.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Variant.QueryServices;

public sealed class VariantQueryService(DBContext context) : IVariantQueryService
{
    public async Task<IReadOnlyList<ProductVariantViewDto>> GetProductVariantsAsync(
        ProductId productId, bool activeOnly, CancellationToken ct = default)
    {
        var query = context.ProductVariants
            .AsNoTracking()
            .Include(v => v.VariantAttributes)
                .ThenInclude(va => va.AttributeValue)
                    .ThenInclude(av => av.AttributeType)
            .Include(v => v.ProductVariantShippings)
            .Where(v => v.ProductId == productId && !v.IsDeleted);

        if (activeOnly)
            query = query.Where(v => v.IsActive);

        var variants = await query.ToListAsync(ct);

        return variants.Select(v => new ProductVariantViewDto
        {
            Id = v.Id.Value,
            ProductId = v.ProductId.Value,
            Sku = v.Sku.Value,
            PurchasePrice = v.PurchasePrice.Amount,
            SellingPrice = v.SellingPrice.Amount,
            OriginalPrice = v.OriginalPrice.Amount,
            Stock = v.StockQuantity,
            IsUnlimited = v.IsUnlimited,
            IsActive = v.IsActive,
            IsInStock = v.IsUnlimited || v.StockQuantity > 0,
            HasDiscount = v.OriginalPrice.Amount > v.SellingPrice.Amount,
            DiscountPercentage = v.OriginalPrice.Amount > 0
                ? Math.Round((v.OriginalPrice.Amount - v.SellingPrice.Amount) / v.OriginalPrice.Amount * 100, 2)
                : 0,
            RowVersion = v.RowVersion.ToBase64(),
            EnabledShippingIds = v.ProductVariantShippings.Select(pvs => pvs.ShippingId.Value).ToList(),
            Attributes = v.VariantAttributes.Select(va => new AttributeValueDto(
                va.AttributeValue.Id.Value,
                va.AttributeValue.AttributeTypeId.Value,
                va.AttributeValue.AttributeType.Name,
                va.AttributeValue.Value,
                va.AttributeValue.DisplayName,
                va.AttributeValue.SortOrder
            )).ToList()
        }).ToList().AsReadOnly();
    }

    public async Task<ProductVariantShippingInfoDto?> GetVariantShippingInfoAsync(
        VariantId variantId, CancellationToken ct = default)
    {
        var variant = await context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Product)
            .Include(v => v.ProductVariantShippings)
                .ThenInclude(pvs => pvs.Shipping)
            .FirstOrDefaultAsync(v => v.Id == variantId, ct);

        if (variant is null) return null;

        return new ProductVariantShippingInfoDto
        {
            VariantId = variant.Id.Value,
            ProductName = variant.Product?.Name.Value ?? string.Empty,
            VariantDisplayName = variant.Sku.Value,
            AvailableShippings = variant.ProductVariantShippings
                .Where(pvs => pvs.Shipping.IsActive)
                .Select(pvs => new ShippingSelectionDto
                {
                    Id = pvs.Shipping.Id.Value,
                    Name = pvs.Shipping.Name.Value,
                    Cost = pvs.Shipping.Cost.Amount,
                    IsDefault = pvs.Shipping.IsDefault
                }).ToList()
        };
    }

    public async Task<VariantAvailabilityDto?> GetVariantAvailabilityAsync(
        VariantId variantId, CancellationToken ct = default)
    {
        var variant = await context.ProductVariants
            .AsNoTracking()
            .Where(v => v.Id == variantId && !v.IsDeleted)
            .Select(v => new VariantAvailabilityDto
            {
                VariantId = v.Id.Value,
                IsActive = v.IsActive,
                IsInStock = v.IsUnlimited || v.StockQuantity > 0,
                StockQuantity = v.StockQuantity,
                IsUnlimited = v.IsUnlimited
            })
            .FirstOrDefaultAsync(ct);

        return variant;
    }

    Task<IEnumerable<ProductVariantViewDto>> IVariantQueryService.GetProductVariantsAsync(ProductId productId, bool activeOnly, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}