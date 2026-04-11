using Application.Product.Features.Queries.GetAdminProducts;
using Mapster;

public sealed class ProductQueryMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<AdminProductSearchRequest, GetAdminProductsQuery>();
    }
}