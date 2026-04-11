using Application.Brand.Features.Shared;
using Domain.Brand.Aggregates;
using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Mapster;

namespace Application.Brand.Mapping;

public sealed class BrandQueryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<BrandId, Guid>()
            .MapWith(src => src.Value);

        config.NewConfig<CategoryId, Guid>()
            .MapWith(src => src.Value);

        config.NewConfig<Brand, BrandDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.CategoryId, src => src.CategoryId.Value)
            .Map(dest => dest.Name, src => src.Name.Value)
            .Map(dest => dest.Slug, src => src.Slug.Value)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.LogoPath, src => src.LogoPath)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .IgnoreNonMapped(true);

        config.NewConfig<Brand, BrandDetailDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.CategoryId, src => src.CategoryId.Value)
            .Map(dest => dest.Name, src => src.Name.Value)
            .Map(dest => dest.Slug, src => src.Slug.Value)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.LogoPath, src => src.LogoPath)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
            .Map(dest => dest.CategoryName, src => string.Empty)
            .Map(dest => dest.ProductCount, src => 0)
            .Map(dest => dest.ActiveProductCount, src => 0)
            .IgnoreNonMapped(true);
    }
}