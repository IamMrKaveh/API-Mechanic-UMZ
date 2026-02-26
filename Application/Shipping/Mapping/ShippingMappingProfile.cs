namespace Application.Shipping.Mapping;

public class ShippingMappingProfile : Profile
{
    public ShippingMappingProfile()
    {
        CreateMap<Domain.Shipping.Shipping, ShippingDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Cost, opt => opt.MapFrom(src => src.BaseCost.Amount))
            .ForMember(dest => dest.EstimatedDeliveryTime, opt => opt.MapFrom(src => src.EstimatedDeliveryTime))
            .ForMember(dest => dest.MinDeliveryDays, opt => opt.MapFrom(src => src.MinDeliveryDays))
            .ForMember(dest => dest.MaxDeliveryDays, opt => opt.MapFrom(src => src.MaxDeliveryDays))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.IsDeleted))
            .ForMember(dest => dest.IsDefault, opt => opt.MapFrom(src => src.IsDefault))
            .ForMember(dest => dest.SortOrder, opt => opt.MapFrom(src => src.SortOrder))
            .ForMember(dest => dest.MinOrderAmount, opt => opt.MapFrom(src => src.MinOrderAmount))
            .ForMember(dest => dest.MaxOrderAmount, opt => opt.MapFrom(src => src.MaxOrderAmount))
            .ForMember(dest => dest.IsFreeAboveAmount, opt => opt.MapFrom(src => src.IsFreeAboveAmount))
            .ForMember(dest => dest.FreeShippingThreshold, opt => opt.MapFrom(src => src.FreeShippingThreshold))
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : null));
    }
}