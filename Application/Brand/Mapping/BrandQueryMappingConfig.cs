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

        config.NewConfig<Domain.Brand.Aggregates.Brand, BrandViewDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.CategoryId, src => src.CategoryId.Value)
            .Map(dest => dest.Name, src => src.Name.Value)
            .Map(dest => dest.Slug, src => src.Slug.Value)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Ignore(dest => dest.CategoryName)
            .Ignore(dest => dest.ActiveProductsCount)
            .Ignore(dest => dest.TotalProductsCount)
            .IgnoreNonMapped(true);
    }
}