using Application.Product.Features.Queries.GetProduct;
using Application.Product.Features.Queries.GetProductDetails;

namespace Presentation.Product.Mapping;

public static class ProductMappingExtensions
{
    public static GetProductQuery Enrich(
        this GetProductQuery query,
        Guid productId) => query with
        {
            Id = productId
        };

    public static GetProductDetailsQuery Enrich(
        this GetProductDetailsQuery query,
        Guid productId) => query with
        {
            ProductId = productId
        };
}