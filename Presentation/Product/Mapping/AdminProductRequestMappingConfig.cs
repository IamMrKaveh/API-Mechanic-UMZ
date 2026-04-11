using Application.Product.Features.Queries.GetAdminProducts;
using Mapster;
using Presentation.Product.Requests;

namespace Presentation.Product.Mapping;

public sealed class AdminProductRequestMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GetAdminProductsRequest, GetAdminProductsQuery>()
            .Map(dest => dest.CategoryId, src => src.CategoryId)
            .Map(dest => dest.BrandId, src => src.BrandId)
            .Map(dest => dest.Search, src => src.Search)
            .Map(dest => dest.IsActive, src => src.IsActive)
            .Map(dest => dest.IncludeDeleted, src => src.IncludeDeleted)
            .Map(dest => dest.Page, src => src.Page)
            .Map(dest => dest.PageSize, src => src.PageSize)
            .Ignore(dest => dest.UserId);
    }
}