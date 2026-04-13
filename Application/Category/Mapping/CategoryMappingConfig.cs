using Application.Category.Features.Shared;
using Domain.Category.Aggregates;
using Domain.Category.ValueObjects;
using Mapster;

namespace Application.Category.Mapping;

public class CategoryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Domain.Category.Aggregates.Category, CategoryDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name.Value)
            .Map(dest => dest.Slug, src => src.Slug.Value)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .IgnoreNonMapped(true);
    }
}