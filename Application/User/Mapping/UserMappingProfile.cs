using Application.Order.Features.Shared;
using Application.User.Features.Shared;
using Domain.User.Entities;

namespace Application.User.Mapping;

public sealed class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<Domain.User.Aggregates.User, UserProfileDto>()
            .ForMember(dest => dest.UserAddresses,
                opt => opt.MapFrom(src => src.UserAddresses.Where(a => !a.IsDeleted)));

        CreateMap<UserAddress, UserAddressDto>();

        CreateMap<Domain.User.Aggregates.User, UserSummaryDto>()
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber));
    }
}