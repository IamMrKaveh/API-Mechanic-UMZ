using Application.Brand.Features.Commands.CreateBrand;
using Application.Brand.Features.Commands.MoveBrand;
using Application.Brand.Features.Commands.UpdateBrand;
using Application.Brand.Features.Shared;
using Domain.Brand.Aggregates;
using Mapster;

namespace Application.Brand.Mapping;

public class BrandMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Brand, BrandDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name.Value)
            .Map(dest => dest.Slug, src => src.Slug.Value)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.LogoPath, src => src.LogoPath)
            .Map(dest => dest.CategoryId, src => src.CategoryId.Value)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

        config.NewConfig<Brand, BrandDetailDto>()
            .Map(dest => dest.Id, src => src.Id.Value)
            .Map(dest => dest.Name, src => src.Name.Value)
            .Map(dest => dest.Slug, src => src.Slug.Value)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.CategoryId, src => src.CategoryId.Value)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.CreatedAt, src => src.CreatedAt)
            .Ignore(dest => dest.CategoryName)
            .Ignore(dest => dest.ProductCount)
            .Ignore(dest => dest.ActiveProductCount);

        config.NewConfig<CreateBrandDto, CreateBrandCommand>()
            .Map(dest => dest.CategoryId, src => src.CategoryId)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Slug, src => src.Slug)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.LogoFile, src => (Stream?)null);

        config.NewConfig<UpdateBrandDto, UpdateBrandCommand>()
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.CategoryId, src => src.CategoryId)
            .Map(dest => dest.Slug, src => src.Slug)
            .Map(dest => dest.Description, src => src.Description)
            .Map(dest => dest.LogoFile, src => (Stream?)null)
            .Map(dest => dest.RowVersion, src => src.RowVersion)
            .IgnoreNonMapped(true);

        config.NewConfig<MoveBrandDto, MoveBrandCommand>()
            .Map(dest => dest.BrandId, src => src.BrandId)
            .Map(dest => dest.TargetCategoryId, src => src.TargetCategoryId);
    }
}