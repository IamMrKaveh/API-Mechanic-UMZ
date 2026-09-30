using Application.Shipping.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Shipping.Features.Queries.GetAvailableShippings;

[RequestOutputCache(
    tags:
    [
        CacheTags.Shipping
    ],
    expirationInSeconds: 300)]
public sealed record GetAvailableShippingsQuery(
    decimal OrderAmount)
    : IQuery<IReadOnlyList<AvailableShippingDto>>;