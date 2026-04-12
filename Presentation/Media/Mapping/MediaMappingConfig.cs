using Application.Media.Features.Queries.GetAllMedia;
using Mapster;
using Presentation.Media.Requests;

namespace Presentation.Media.Mapping;

public sealed class MediaMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GetAllMediaRequest, GetAllMediaQuery>();
    }
}