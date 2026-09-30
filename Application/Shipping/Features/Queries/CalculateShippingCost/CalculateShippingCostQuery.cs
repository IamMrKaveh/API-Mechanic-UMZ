using Application.Shipping.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Shipping.Features.Queries.CalculateShippingCost;

[RequestOutputCache(
    tags:
    [
        CacheTags.Shipping
    ],
    expirationInSeconds: 300)]
public sealed record CalculateShippingCostQuery(
    Guid ShippingId,
    decimal OrderAmount)
    : IQuery<ShippingCostResultDto>;