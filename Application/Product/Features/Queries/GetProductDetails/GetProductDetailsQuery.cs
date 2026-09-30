using Application.Cache.Contracts;
using Application.Product.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Product.Features.Queries.GetProductDetails;

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
    expirationInSeconds: 120)]
public record GetProductDetailsQuery(
    Guid ProductId)
    : IQuery<PublicProductDetailDto?>;
