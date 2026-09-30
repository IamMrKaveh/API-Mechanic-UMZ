using Application.Variant.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Variant.Features.Queries.GetVariants;

[RequestOutputCache(
    tags:
    [
        CacheTags.ProductVariant,
        CacheTags.Product,
        CacheTags.Inventory
    ],
    expirationInSeconds: 60)]
public record GetVariantsQuery(
    Guid ProductId,
    bool ActiveOnly = true)
    : IQuery<IEnumerable<ProductVariantViewDto>>;