using Application.Brand.Features.Shared;
using Mapster;

namespace Application.Brand.Mapping;

public sealed class BrandProjectionConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Domain.Brand.Aggregates.Brand, BrandListItemDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.CategoryId, src => src.CategoryId.Value)
            .Map(dest => dest.Name, src => src.Name.Value)
            .Map(dest => dest.Slug, src => src.Slug.Value)
            .Map(dest => dest.LogoPath, src => src.LogoPath)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.CategoryName, src => string.Empty)
            .Map(dest => dest.ProductCount, src => 0)
            .IgnoreNonMapped(true);
    }
}