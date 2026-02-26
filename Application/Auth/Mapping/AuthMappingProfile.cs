namespace Application.Auth.Mapping;

public class AuthMappingProfile : Profile
{
    public AuthMappingProfile()
    {
        CreateMap<UserSession, UserSessionDto>()
            .ForMember(dest => dest.DeviceInfo,
                opt => opt.MapFrom(src => UserAgentHelper.GetDeviceInfo(src.UserAgent)))
            .ForMember(dest => dest.BrowserInfo,
                opt => opt.MapFrom(src => UserAgentHelper.GetBrowserInfo(src.UserAgent)))
            .ForMember(dest => dest.IsCurrent, opt => opt.Ignore());
    }
}