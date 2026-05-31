using Application.Variant.Features.Shared;
using Domain.Variant.Aggregates;

namespace Application.Variant.Mapping;

public class VariantMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ProductVariant, ProductVariantViewDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Sku, src => src.Sku.Value)
            .Map(dest => dest.Price, src => src.Price.Amount)
            .Map(dest => dest.SellingPrice, src => src.Price.Amount)
            .Map(dest => dest.OriginalPrice, src => src.CompareAtPrice != null ? src.CompareAtPrice.Amount : src.Price.Amount)
            .Map(dest => dest.CompareAtPrice, src => src.CompareAtPrice != null ? src.CompareAtPrice.Amount : (decimal?)null)
            .Map(dest => dest.HasDiscount, src => src.IsDiscounted)
            .Map(dest => dest.DiscountPercentage, src => src.DiscountPercentage ?? 0m)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Ignore(dest => dest.Stock)
            .Ignore(dest => dest.IsUnlimited)
            .Ignore(dest => dest.IsInStock)
            .Ignore(dest => dest.PurchasePrice)
            .Ignore(dest => dest.EnabledShippingIds)
            .Ignore(dest => dest.ShippingMultiplier)
            .Ignore(dest => dest.Attributes)
            .Ignore(dest => dest.Images)
            .Ignore(dest => dest.RowVersion);
    }
}