using Application.Cache.Contracts;
using Application.Order.Features.Shared;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Order.Features.Queries.GetOrderStatuses;

[RequestOutputCache(
    tags:
    [
        CacheTags.OrderStatus
    ],
    expirationInSeconds: 600)]
public record GetOrderStatusesQuery(
    bool? OnlyActive = null)
    : IQuery<IReadOnlyList<OrderStatusDto>>;
