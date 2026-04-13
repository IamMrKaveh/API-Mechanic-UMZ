using Application.Media.Features.Shared;
using Mapster;

namespace Application.Media.Mapping;

public class MediaMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Domain.Media.Aggregates.Media, MediaDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.FilePath, src => src.FilePath)
            .Map(dest => dest.FileName, src => src.FileName)
            .Map(dest => dest.FileType, src => src.FileType)
            .Map(dest => dest.FileSize, src => src.FileSize)
            .Map(dest => dest.EntityType, src => src.EntityType)
            .Map(dest => dest.EntityId, src => src.EntityId)
            .Map(dest => dest.SortOrder, src => src.SortOrder)
            .Map(dest => dest.IsPrimary, src => src.IsPrimary)
            .Map(dest => dest.AltText, src => src.AltText)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt);
    }
}