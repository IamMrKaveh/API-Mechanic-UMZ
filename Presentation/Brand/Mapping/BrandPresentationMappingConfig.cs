using Application.Brand.Features.Commands.MoveBrand;
using Application.Brand.Features.Queries.GetAdminBrands;
using Application.Brand.Features.Queries.GetPublicBrands;
using Mapster;
using Presentation.Brand.Requests;

namespace Presentation.Brand.Mapping;

public sealed class BrandPresentationMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<MoveBrandRequest, MoveBrandCommand>()
            .Map(dest => dest.BrandId, src => src.BrandId)
            .Map(dest => dest.TargetCategoryId, src => src.TargetCategoryId)
            .IgnoreNonMapped(true);

        config.NewConfig<GetAdminBrandsRequest, GetAdminBrandsQuery>()
            .Map(dest => dest.CategoryId, src => src.CategoryId)
            .Map(dest => dest.Search, src => src.Search)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.IncludeDeleted, src => src.IncludeDeleted)
            .Map(dest => dest.Page, src => src.Page)
            .Map(dest => dest.PageSize, src => src.PageSize)
            .IgnoreNonMapped(true);

        config.NewConfig<GetPublicBrandsRequest, GetPublicBrandsQuery>()
            .Map(dest => dest.CategoryId, src => src.CategoryId)
            .IgnoreNonMapped(true);
    }
}