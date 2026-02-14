namespace Application.Discount.Features.Shared;

public class DiscountMappingProfile : Profile
{
    public DiscountMappingProfile()
    {
        CreateMap<DiscountCode, DiscountCodeDto>()
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src =>
                src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : null));

        CreateMap<DiscountCode, DiscountCodeDetailDto>()
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src =>
                src.RowVersion != null ? Convert.ToBase64String(src.RowVersion) : null))
            .ForMember(dest => dest.Restrictions, opt => opt.MapFrom(src => src.Restrictions))
            .ForMember(dest => dest.RecentUsages, opt => opt.MapFrom(src => src.Usages));

        CreateMap<DiscountRestriction, DiscountRestrictionDto>()
            .ForMember(dest => dest.RestrictionType, opt => opt.MapFrom(src => src.Type.ToString()));

        CreateMap<DiscountUsage, DiscountUsageDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src =>
                src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null));
    }
}