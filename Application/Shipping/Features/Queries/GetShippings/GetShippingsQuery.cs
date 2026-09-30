using Application.Shipping.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Shipping.Features.Queries.GetShippings;

[RequestOutputCache(
    tags:
    [
        CacheTags.Shipping
    ],
    expirationInSeconds: 1800)]
public record GetShippingsQuery(
    bool IncludeInactive = false)
    : IQuery<IReadOnlyList<ShippingListItemDto>>;
