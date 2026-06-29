using Application.Cache.Contracts;
using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetOrderStatuses;

public record GetOrderStatusesQuery(
    bool? OnlyActive = null)
    : IQuery<IReadOnlyList<OrderStatusDto>>, ICacheableQuery
{
    public string CacheKey => $"order-status:list:onlyActive={OnlyActive?.ToString() ?? "all"}";
    public TimeSpan? Expiry => TimeSpan.FromMinutes(10);
}