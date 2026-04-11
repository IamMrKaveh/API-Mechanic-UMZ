using Application.Brand.Features.Shared;
using Domain.Brand.Aggregates;
using Mapster;

namespace Application.Brand.Mapping;

public sealed class BrandProjectionConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Brand, BrandListItemDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.CategoryId, src => src.CategoryId.Value)
            .Map(dest => dest.Name, src => src.Name.Value)
            .Map(dest => dest.Slug, src => src.Slug.Value)
            .Map(dest => dest.Description, src => (string?)null)
            .Map(dest => dest.LogoPath, src => src.LogoPath)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.CategoryName, src => string.Empty)
            .Map(dest => dest.ProductCount, src => 0)
            .IgnoreNonMapped(true);
    }
}