using Application.Attribute.Features.Shared;
using Domain.Attribute.Entities;
using Domain.Variant.Aggregates;

namespace Application.Variant.Features.Shared;

public record VariantStockChangedApplicationNotification : INotification
{
    public Guid VariantId { get; init; }
    public Guid ProductId { get; init; }
    public int QuantityChanged { get; init; }
    public int NewOnHand { get; init; }
    public int NewReserved { get; init; }
    public int NewAvailable { get; init; }
    public bool IsInStock { get; init; }
}

public sealed record ProductVariantViewDto
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string Sku { get; init; } = string.Empty;
    public decimal SellingPrice { get; init; }
    public decimal OriginalPrice { get; init; }
    public bool IsActive { get; init; }
    public bool HasDiscount { get; init; }
    public decimal DiscountPercentage { get; init; }
    public int Stock { get; init; }
    public int StockQuantity { get; init; }
    public int AvailableQuantity { get; init; }
    public int ReservedQuantity { get; init; }
    public bool IsUnlimited { get; init; }
    public bool IsInStock { get; init; }
    public decimal ShippingMultiplier { get; init; } = 1m;
    public List<Guid> EnabledShippingIds { get; init; } = [];
    public Dictionary<string, AttributeValueDto> Attributes { get; init; } = [];
}

public static class ProductVariantViewDtoFactory
{
    public static ProductVariantViewDto Create(
        ProductVariant variant,
        Domain.Inventory.Aggregates.Inventory inventory,
        List<AttributeValue> attributeValues)
    {
        var attributesDict = variant.Attributes.ToDictionary(
            va =>
            {
                var av = attributeValues.FirstOrDefault(x => x.Id == va.ValueId);
                return av?.AttributeType?.Name ?? va.AttributeTypeId.Value.ToString();
            },
            va =>
            {
                var av = attributeValues.FirstOrDefault(x => x.Id == va.ValueId);
                return new AttributeValueDto
                {
                    Id = va.ValueId.Value,
                    AttributeTypeId = va.AttributeTypeId.Value,
                    Value = av?.Value ?? va.DisplayValue,
                    DisplayValue = va.DisplayValue,
                    HexCode = av?.HexCode,
                    SortOrder = av?.SortOrder ?? 0,
                    IsActive = av?.IsActive ?? true
                };
            });

        return new ProductVariantViewDto
        {
            Id = variant.Id.Value,
            ProductId = variant.ProductId.Value,
            Sku = variant.Sku.Value,
            SellingPrice = variant.SellingPrice.Amount,
            OriginalPrice = variant.OriginalPrice.Amount,
            IsActive = variant.IsActive,
            HasDiscount = variant.IsDiscounted,
            DiscountPercentage = variant.DiscountPercentage ?? 0m,
            Stock = inventory.StockQuantity,
            StockQuantity = inventory.StockQuantity,
            IsUnlimited = inventory.IsUnlimited,
            IsInStock = inventory.IsInStock,
            EnabledShippingIds = variant.Shippings.Select(s => s.ShippingId.Value).ToList(),
            ShippingMultiplier = variant.Shippings.Count > 0
                ? variant.Shippings.Min(s => s.ShippingMultiplier)
                : 1m,
            Attributes = attributesDict
        };
    }
}