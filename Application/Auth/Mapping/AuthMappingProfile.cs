namespace Application.Auth.Mapping;

public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<Domain.User.User, UserProfileDto>()
            .ForMember(dest => dest.UserAddresses,
                opt => opt.MapFrom(src => src.UserAddresses.Where(a => !a.IsDeleted)));

        CreateMap<UserAddress, UserAddressDto>();
    }
}