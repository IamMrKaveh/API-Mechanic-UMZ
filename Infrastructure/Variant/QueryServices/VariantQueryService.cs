using Application.Attribute.Features.Shared;
using Application.Shipping.Features.Shared;
using Application.Variant.Contracts;
using Application.Variant.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;

namespace Infrastructure.Variant.QueryServices;

public sealed class VariantQueryService(DBContext context) : IVariantQueryService
{
    public async Task<IEnumerable<ProductVariantViewDto>> GetProductVariantsAsync(
        ProductId productId, bool activeOnly, CancellationToken ct = default)
    {
        var query = context.ProductVariants
            .AsNoTracking()
            .Where(v => v.ProductId == productId && !v.IsDeleted)
            .Include(v => v.Attributes)
                .ThenInclude(va => va.AttributeType)
            .Include(v => v.Attributes)
                .ThenInclude(va => va.Value)
            .Include(v => v.Shippings)
            .AsSplitQuery();

        if (activeOnly)
            query = query.Where(v => v.IsActive);

        var variants = await query.ToListAsync(ct);

        return variants.Select(v => new ProductVariantViewDto
        {
            Id = v.Id.Value,
            Sku = v.Sku.Value,
            SellingPrice = v.SellingPrice.Amount,
            OriginalPrice = v.OriginalPrice.Amount,
            IsActive = v.IsActive,
            IsInStock = false,
            HasDiscount = v.IsDiscounted,
            DiscountPercentage = v.DiscountPercentage ?? 0m,
            EnabledShippingIds = v.Shippings
                .Select(s => s.ShippingId.Value)
                .ToList(),
            ShippingMultiplier = v.Shippings.Count > 0
                ? v.Shippings.Min(s => s.ShippingMultiplier)
                : 1m,
            Attributes = v.Attributes.ToDictionary(
                va => va.AttributeType?.Name ?? va.AttributeTypeId.Value.ToString(),
                va => new AttributeValueDto
                {
                    Id = va.Value?.Id.Value ?? Guid.Empty,
                    AttributeTypeId = va.AttributeTypeId.Value,
                    Value = va.Value?.Value ?? va.DisplayValue,
                    DisplayValue = va.DisplayValue,
                    HexCode = va.Value?.HexCode,
                    SortOrder = va.Value?.SortOrder ?? 0,
                    IsActive = va.Value?.IsActive ?? true
                })
        }).ToList();
    }

    public async Task<VariantShippingInfoDto?> GetVariantShippingInfoAsync(
        VariantId variantId, CancellationToken ct = default)
    {
        var variant = await context.ProductVariants
            .AsNoTracking()
            .AsSplitQuery()
            .Where(v => v.Id == variantId && !v.IsDeleted)
            .Include(v => v.Shippings)
                .ThenInclude(pvs => pvs.Shipping)
            .FirstOrDefaultAsync(ct);

        if (variant is null) return null;

        var activeShippings = variant.Shippings
            .Where(pvs => pvs.Shipping is not null && pvs.Shipping.IsActive)
            .ToList();

        return new VariantShippingInfoDto
        {
            VariantId = variant.Id.Value,
            ShippingMultiplier = activeShippings.Count > 0
                ? activeShippings.Min(pvs => pvs.ShippingMultiplier)
                : 1m,
            AvailableShippings = activeShippings
                .Select(pvs => new ShippingListItemDto
                {
                    Id = pvs.Shipping.Id.Value,
                    Name = pvs.Shipping.Name.Value,
                    BaseCost = pvs.Shipping.Cost.Amount,
                    IsActive = pvs.Shipping.IsActive,
                    IsDefault = pvs.Shipping.IsDefault
                })
                .ToList()
        };
    }
}