using Application.Product.Features.Queries.GetProduct;
using Application.Product.Features.Queries.GetProducts;
using Mapster;
using Presentation.Product.Requests;

namespace Presentation.Product.Mapping;

public sealed class ProductRequestMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GetProductsRequest, GetProductsQuery>();

        config.NewConfig<GetProductRequest, GetProductQuery>()
            .Ignore(dest => dest.Id);
    }
}