using Application.Order.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Order.Features.Queries.GetOrderStatus;

[RequestOutputCache(
    tags:
    [
        CacheTags.OrderStatus
    ],
    expirationInSeconds: 600)]
public record GetOrderStatusQuery(
    Guid Id)
    : IQuery<OrderStatusDto>;