namespace Application.Media.Mapping;

public class MediaMappingProfile : Profile
{
    public MediaMappingProfile()
    {
        CreateMap<Domain.Media.Media, MediaDto>()
            .ForMember(dest => dest.Url, opt => opt.Ignore());

        CreateMap<Domain.Media.Media, MediaDetailDto>()
            .ForMember(dest => dest.Url, opt => opt.Ignore())
            .ForMember(dest => dest.FileSizeDisplay,
                opt => opt.MapFrom(src => FileSizeFormatter.Format(src.FileSize)));

        CreateMap<Domain.Media.Media, MediaListItemDto>()
            .ForMember(dest => dest.Url, opt => opt.Ignore())
            .ForMember(dest => dest.FileSizeDisplay,
                opt => opt.MapFrom(src => FileSizeFormatter.Format(src.FileSize)));
    }
}