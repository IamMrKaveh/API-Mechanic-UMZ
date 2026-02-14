namespace Application.Cart.Features.Shared;

public class CartMappingProfile : Profile
{
    public CartMappingProfile()
    {
        // Cart → CartDetailDto (ساده - بدون اطلاعات محصول، آن‌ها در QueryService پر می‌شوند)
        CreateMap<Domain.Cart.Cart, CartDetailDto>()
            .ForMember(d => d.Items, opt => opt.Ignore()) // در QueryService پر می‌شود
            .ForMember(d => d.PriceChanges, opt => opt.Ignore());
    }
}