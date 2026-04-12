using Application.Variant.Features.Commands.AddVariant;
using Application.Variant.Features.Commands.UpdateVariant;
using Mapster;
using Presentation.Variant.Requests;

namespace Presentation.Variant.Mapping;

public sealed class VariantMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AddVariantRequest, AddVariantCommand>()
            .Map(dest => dest.Sku, src => src.Sku)
            .Map(dest => dest.PurchasePrice, src => src.PurchasePrice)
            .Map(dest => dest.SellingPrice, src => src.SellingPrice)
            .Map(dest => dest.OriginalPrice, src => src.OriginalPrice)
            .Map(dest => dest.Stock, src => src.Stock)
            .Map(dest => dest.IsUnlimited, src => src.IsUnlimited)
            .Map(dest => dest.ShippingMultiplier, src => src.ShippingMultiplier)
            .Map(dest => dest.AttributeValueIds, src => src.AttributeValueIds != null
                ? src.AttributeValueIds.ToList()
                : new List<Guid>())
            .Map(dest => dest.EnabledShippingIds, src => src.EnabledShippingIds)
            .Ignore(dest => dest.ProductId)
            .IgnoreNonMapped(true);

        config.NewConfig<UpdateVariantRequest, UpdateVariantCommand>()
            .Map(dest => dest.Sku, src => src.Sku)
            .Map(dest => dest.PurchasePrice, src => src.PurchasePrice)
            .Map(dest => dest.SellingPrice, src => src.SellingPrice)
            .Map(dest => dest.OriginalPrice, src => src.OriginalPrice)
            .Map(dest => dest.Stock, src => src.Stock)
            .Map(dest => dest.IsUnlimited, src => src.IsUnlimited)
            .Map(dest => dest.ShippingMultiplier, src => src.ShippingMultiplier)
            .Map(dest => dest.AttributeValueIds, src => src.AttributeValueIds)
            .Map(dest => dest.EnabledShippingMethodIds, src => src.EnabledShippingMethodIds)
            .Map(dest => dest.ProductId, src => src.ProductId)
            .Map(dest => dest.VariantId, src => src.VariantId)
            .Map(dest => dest.UserId, src => src.UserId)
            .IgnoreNonMapped(true);
    }
}