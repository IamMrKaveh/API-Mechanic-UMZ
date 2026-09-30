using Application.Shipping.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Shipping.Features.Queries.GetShipping;

[RequestOutputCache(
    tags:
    [
        CacheTags.Shipping
    ],
    expirationInSeconds: 300)]
public record GetShippingQuery(
    Guid Id)
    : IQuery<ShippingDto>;