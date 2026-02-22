namespace Application.Media.Mapping;

public class MediaMappingProfile : Profile
{
    public MediaMappingProfile()
    {
        CreateMap<Domain.Media.Media, MediaDto>();
        CreateMap<Domain.Media.Media, MediaDetailDto>();
        CreateMap<Domain.Media.Media, MediaListItemDto>();
    }
}