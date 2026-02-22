namespace Application.Cart.Mapping;

public class CartMappingProfile : Profile
{
    public CartMappingProfile()
    {
        CreateMap<Domain.Cart.Cart, CartDetailDto>()
            .ForMember(d => d.Items, opt => opt.Ignore())
            .ForMember(d => d.PriceChanges, opt => opt.Ignore());

        CreateMap<Domain.Cart.CartItem, CartItemDto>();
    }
}