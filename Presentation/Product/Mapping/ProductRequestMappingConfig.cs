using Application.Product.Features.Queries.GetProductCatalog;
using Application.Product.Features.Queries.GetProducts;
using Mapster;
using Presentation.Product.Requests;

namespace Presentation.Product.Mapping;

public sealed class ProductRequestMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GetProductsRequest, GetProductsQuery>();
        config.NewConfig<GetProductCatalogRequest, GetProductCatalogQuery>();
    }
}