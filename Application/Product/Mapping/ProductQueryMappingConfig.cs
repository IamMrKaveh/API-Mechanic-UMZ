using Application.Product.Features.Queries.GetProductCatalog;
using Application.Product.Features.Shared;
using Mapster;

namespace Application.Product.Mapping;

public sealed class ProductQueryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ProductCatalogSearchParams, GetProductCatalogQuery>()
            .Map(dest => dest.Page, src => src.Page)
            .Map(dest => dest.PageSize, src => src.PageSize)
            .Map(dest => dest.Search, src => src.Search)
            .Map(dest => dest.CategoryId, src => src.CategoryId)
            .Map(dest => dest.BrandId, src => src.BrandId)
            .Map(dest => dest.MinPrice, src => src.MinPrice)
            .Map(dest => dest.MaxPrice, src => src.MaxPrice)
            .Map(dest => dest.InStockOnly, src => src.InStockOnly)
            .Map(dest => dest.SortBy, src => src.SortBy);
    }
}