namespace Application.User.Mapping;

public sealed class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<Domain.User.User, UserProfileDto>()
            .ForMember(dest => dest.UserAddresses,
                opt => opt.MapFrom(src => src.UserAddresses.Where(a => !a.IsDeleted)));

        CreateMap<UserAddress, UserAddressDto>();

        CreateMap<Domain.User.User, UserSummaryDto>();
    }
}