using Application.Order.Features.Shared;
using Application.Cache.Contracts;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace Application.Order.Features.Queries.GetOrderStatistics;

[RequestOutputCache(
    tags:
    [
        CacheTags.Order,
        CacheTags.OrderItem
    ],
    expirationInSeconds: 60)]
public record GetOrderStatisticsQuery
    : IQuery<OrderStatisticsDto>;