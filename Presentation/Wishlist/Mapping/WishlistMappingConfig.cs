using Application.Wishlist.Features.Commands.ToggleWishlist;
using Mapster;
using Presentation.Wishlist.Requests;

namespace Presentation.Wishlist.Mapping;

public sealed class WishlistMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ToggleWishlistRequest, ToggleWishlistCommand>()
            .Map(dest => dest.ProductId, src => src.ProductId)
            .Ignore(dest => dest.UserId)
            .IgnoreNonMapped(true);
    }
}