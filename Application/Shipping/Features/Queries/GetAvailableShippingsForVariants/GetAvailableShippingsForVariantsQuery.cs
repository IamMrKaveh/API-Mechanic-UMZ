using Application.Shipping.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Shipping.Features.Queries.GetAvailableShippingsForVariants;

[RequestOutputCache(
    tags:
    [
        CacheTags.Shipping,
        CacheTags.VariantShipping
    ],
    expirationInSeconds: 300)]
public sealed record GetAvailableShippingsForVariantsQuery(
    ICollection<Guid> VariantIds)
    : IQuery<IReadOnlyList<AvailableShippingDto>>;