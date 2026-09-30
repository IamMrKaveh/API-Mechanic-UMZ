using Application.Product.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Product.Features.Queries.GetProduct;

[RequestOutputCache(
    tags:
    [
        CacheTags.Product,
        CacheTags.ProductVariant,
        CacheTags.Category,
        CacheTags.Brand,
        CacheTags.Inventory,
        CacheTags.Media
    ],
    expirationInSeconds: 600)]
public record GetProductQuery(Guid Id)
    : IQuery<ProductDetailDto>;
