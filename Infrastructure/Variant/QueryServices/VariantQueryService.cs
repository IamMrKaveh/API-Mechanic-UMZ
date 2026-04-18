using Application.Attribute.Features.Shared;
using Application.Shipping.Features.Shared;
using Application.Variant.Contracts;
using Application.Variant.Features.Shared;
using Domain.Product.ValueObjects;
using Domain.Variant.ValueObjects;
using Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Variant.QueryServices;

public sealed class VariantQueryService(DBContext context) : IVariantQueryService
{
    public async Task<IEnumerable<ProductVariantViewDto>> GetProductVariantsAsync(
        ProductId productId, bool activeOnly, CancellationToken ct = default)
    {
        var query = context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Attributes)
                .ThenInclude(va => va.Value)
                    .ThenInclude(av => av.AttributeType)
            .Include(v => v.Shippings)
            .Where(v => v.ProductId == productId && !v.IsDeleted);

        if (activeOnly)
            query = query.Where(v => v.IsActive);

        var variants = await query.ToListAsync(ct);

        return variants.Select(v => new ProductVariantViewDto
        {
            Id = v.Id.Value,
            Sku = v.Sku.Value,
            PurchasePrice = v.Price.Amount,
            SellingPrice = v.SellingPrice.Amount,
            OriginalPrice = v.CompareAtPrice?.Amount ?? v.Price.Amount,
            IsActive = v.IsActive,
            IsInStock = false,
            HasDiscount = v.IsDiscounted,
            DiscountPercentage = v.DiscountPercentage ?? 0m,
            EnabledShippingIds = v.Shippings.Select(s => s.ShippingId.Value).ToList(),
            Attributes = v.Attributes.ToDictionary(
                va => va.AttributeType?.Name ?? va.AttributeTypeId.Value.ToString(),
                va => new AttributeValueDto
                {
                    Id = va.Value?.Id.Value ?? Guid.Empty,
                    AttributeTypeId = va.AttributeTypeId.Value,
                    Value = va.DisplayValue,
                    DisplayValue = va.DisplayValue,
                    SortOrder = 0
                })
        });
    }

    public async Task<ProductVariantShippingInfoDto?> GetVariantShippingInfoAsync(
        VariantId variantId, CancellationToken ct = default)
    {
        var variant = await context.ProductVariants
            .AsNoTracking()
            .Include(v => v.Shippings)
                .ThenInclude(pvs => pvs.Shipping)
            .FirstOrDefaultAsync(v => v.Id == variantId, ct);

        if (variant is null) return null;

        return new ProductVariantShippingInfoDto
        {
            VariantId = variant.Id.Value,
            AvailableShippings = variant.Shippings
                .Where(pvs => pvs.Shipping.IsActive)
                .Select(pvs => new ShippingListItemDto
                {
                    Id = pvs.Shipping.Id.Value,
                    Name = pvs.Shipping.Name.Value,
                    BaseCost = pvs.Shipping.Cost.Amount,
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
            .FirstOrDefaultAsync(ct);

        if (variant is null) return null;

        return new VariantAvailabilityDto(
            VariantId: variant.Id.Value,
            IsInStock: false,
            IsUnlimited: false,
            AvailableQuantity: 0,
            StockQuantity: 0,
            ReservedQuantity: 0);
    }
}