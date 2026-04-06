using Application.Cart.Features.Shared;
using Domain.Cart.Entities;
using Mapster;

namespace Application.Cart.Mapping;

public class CartMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Domain.Cart.Aggregates.Cart, CartDetailDto>()
            .Ignore(dest => dest.Items)
            .Ignore(dest => dest.TotalPrice)
            .Ignore(dest => dest.TotalItems)
            .Ignore(dest => dest.PriceChanges);

        config.NewConfig<CartItem, CartItemDto>()
            .Map(dest => dest.SellingPrice, src => src.UnitPrice)
            .Map(dest => dest.TotalPrice, src => src.TotalPrice)
            .Map(dest => dest.ProductName, src =>
                src.Variant != null && src.Variant.Product != null
                    ? src.Variant.Product.Name.Value
                    : null)
            .Ignore(dest => dest.ProductIcon)
            .Ignore(dest => dest.Attributes)
            .Ignore(dest => dest.RowVersion);
    }
}