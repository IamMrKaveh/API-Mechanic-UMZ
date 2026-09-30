using Application.Variant.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Variant.Features.Queries.GetVariantShipping;

[RequestOutputCache(
    tags:
    [
        CacheTags.VariantShipping,
        CacheTags.Shipping,
        CacheTags.ProductVariant
    ],
    expirationInSeconds: 300)]
public record GetVariantShippingQuery(
    Guid VariantId)
    : IQuery<VariantShippingInfoDto>;