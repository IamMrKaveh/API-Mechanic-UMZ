using Application.Shipping.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Shipping.Features.Queries.GetShippingQuotes;

[RequestOutputCache(
    tags:
    [
        CacheTags.Shipping,
        CacheTags.VariantShipping
    ],
    expirationInSeconds: 300)]
public sealed record GetShippingQuotesQuery(
    decimal OrderAmount,
    ICollection<ShippingQuoteItemDto> Items)
    : IQuery<IReadOnlyList<AvailableShippingDto>>;