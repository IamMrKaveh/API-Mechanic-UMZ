namespace Application.Cart.Mapping;

public class CartMappingProfile : Profile
{
    public CartMappingProfile()
    {
        CreateMap<Domain.Cart.Cart, CartDetailDto>()
            .ForMember(dest => dest.Items, opt => opt.Ignore())
            .ForMember(dest => dest.TotalPrice, opt => opt.Ignore())
            .ForMember(dest => dest.TotalItems, opt => opt.Ignore())
            .ForMember(dest => dest.PriceChanges, opt => opt.Ignore());

        CreateMap<Domain.Cart.CartItem, CartItemDto>()
            .ForMember(dest => dest.SellingPrice, opt => opt.MapFrom(src => src.SellingPrice))
            .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice))
            .ForMember(dest => dest.ProductName,
                opt => opt.MapFrom(src =>
                    src.Variant != null && src.Variant.Product != null
                        ? src.Variant.Product.Name.Value
                        : null))
            .ForMember(dest => dest.ProductIcon, opt => opt.Ignore())
            .ForMember(dest => dest.Attributes, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore());
    }
}