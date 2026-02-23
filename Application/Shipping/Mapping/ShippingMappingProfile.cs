namespace Application.Shipping.Mapping;

public class ShippingMappingProfile : Profile
{
    public ShippingMappingProfile()
    {
        CreateMap<Domain.Shipping.Shipping, ShippingDto>()
            .ForMember(dest => dest.Cost, opt => opt.MapFrom(src => src.BaseCost.Amount))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion));
    }
}