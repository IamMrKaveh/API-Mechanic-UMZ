using Application.Cart.Features.Commands.AddToCart;
using Application.Cart.Features.Commands.UpdateCartItem;
using Application.Cart.Features.Shared;
using Domain.Cart.Aggregates;
using Domain.Cart.Entities;
using Mapster;

namespace Application.Cart.Mapping;

public class CartMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Cart, CartDetailDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.UserId, src => src.UserId != null ? src.UserId.Value : (Guid?)null)
            .Map(dest => dest.GuestToken, src => src.GuestToken != null ? src.GuestToken.Value : null)
            .Map(dest => dest.IsCheckedOut, src => src.IsCheckedOut)
            .Map(dest => dest.TotalPrice, src => src.TotalAmount.Amount)
            .Map(dest => dest.TotalItems, src => src.Items.Sum(i => i.Quantity))
            .Ignore(dest => dest.Items)
            .Ignore(dest => dest.PriceChanges);

        config.NewConfig<CartItem, CartItemDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.CartId, src => src.CartId.Value)
            .Map(dest => dest.VariantId, src => src.VariantId.Value)
            .Map(dest => dest.ProductId, src => src.ProductId.Value)
            .Map(dest => dest.ProductName, src => src.ProductName.Value)
            .Map(dest => dest.Sku, src => src.Sku.Value)
            .Map(dest => dest.UnitPrice, src => src.UnitPrice.Amount)
            .Map(dest => dest.OriginalPrice, src => src.OriginalPrice.Amount)
            .Map(dest => dest.Quantity, src => src.Quantity)
            .Map(dest => dest.TotalPrice, src => src.TotalPrice.Amount)
            .Map(dest => dest.AddedAt, src => src.AddedAt)
            .Ignore(dest => dest.ProductIcon)
            .Ignore(dest => dest.Attributes);

        config.NewConfig<AddToCartDto, AddToCartCommand>()
           .Map(dest => dest.VariantId, src => src.VariantId)
           .Map(dest => dest.Quantity, src => src.Quantity)
           .IgnoreNonMapped(true);

        config.NewConfig<UpdateCartItemDto, UpdateCartItemCommand>()
            .Map(dest => dest.Quantity, src => src.Quantity)
            .IgnoreNonMapped(true);
    }
}