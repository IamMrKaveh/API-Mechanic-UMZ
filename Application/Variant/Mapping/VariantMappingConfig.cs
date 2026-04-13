using Application.Variant.Features.Commands.AddVariant;
using Application.Variant.Features.Commands.UpdateProductVariantShipping;
using Application.Variant.Features.Commands.UpdateVariant;
using Application.Variant.Features.Shared;
using Domain.Variant.Aggregates;
using Mapster;

namespace Application.Variant.Mapping;

public class VariantMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ProductVariant, ProductVariantDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.ProductId, src => src.ProductId.Value)
            .Map(dest => dest.Sku, src => src.Sku.Value)
            .Map(dest => dest.Price, src => src.Price.Amount)
            .Map(dest => dest.CompareAtPrice, src => src.CompareAtPrice != null ? src.CompareAtPrice.Amount : (decimal?)null)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.IsDiscounted, src => src.IsDiscounted)
            .Map(dest => dest.DiscountPercentage, src => src.DiscountPercentage)
            .Map(dest => dest.FinalPrice, src => src.Price.Amount)
            .Ignore(dest => dest.StockQuantity);

        config.NewConfig<AddVariantDto, AddVariantCommand>()
            .Map(dest => dest.Sku, src => src.Sku)
            .Map(dest => dest.PurchasePrice, src => src.PurchasePrice)
            .Map(dest => dest.SellingPrice, src => src.SellingPrice)
            .Map(dest => dest.OriginalPrice, src => src.OriginalPrice)
            .Map(dest => dest.Stock, src => src.Stock)
            .Map(dest => dest.IsUnlimited, src => src.IsUnlimited)
            .Map(dest => dest.ShippingMultiplier, src => src.ShippingMultiplier)
            .Map(dest => dest.AttributeValueIds, src => src.AttributeValueIds ?? new List<Guid>())
            .Map(dest => dest.EnabledShippingIds, src => src.EnabledShippingIds)
            .IgnoreNonMapped(true);

        config.NewConfig<UpdateVariantDto, UpdateVariantCommand>()
            .Map(dest => dest.Sku, src => src.Sku)
            .Map(dest => dest.PurchasePrice, src => src.PurchasePrice)
            .Map(dest => dest.SellingPrice, src => src.SellingPrice)
            .Map(dest => dest.OriginalPrice, src => src.OriginalPrice)
            .Map(dest => dest.Stock, src => src.Stock)
            .Map(dest => dest.IsUnlimited, src => src.IsUnlimited)
            .Map(dest => dest.ShippingMultiplier, src => src.ShippingMultiplier)
            .Map(dest => dest.AttributeValueIds, src => src.AttributeValueIds ?? new List<Guid>())
            .Map(dest => dest.EnabledShippingIds, src => src.EnabledShippingIds)
            .IgnoreNonMapped(true);

        config.NewConfig<UpdateVariantShippingDto, UpdateVariantShippingCommand>()
            .Map(dest => dest.ShippingMultiplier, src => src.ShippingMultiplier)
            .Map(dest => dest.EnabledShippingIds, src => src.EnabledShippingIds)
            .IgnoreNonMapped(true);

        config.NewConfig<ProductVariant, ProductVariantViewDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Sku, src => src.Sku.Value)
            .Map(dest => dest.SellingPrice, src => src.Price.Amount)
            .Map(dest => dest.OriginalPrice, src => src.CompareAtPrice != null ? src.CompareAtPrice.Amount : src.Price.Amount)
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