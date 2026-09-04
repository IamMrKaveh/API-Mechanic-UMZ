using Application.Shipping.Features.Shared;

namespace Application.Shipping.Features.Queries.GetShippings;

public record GetShippingsQuery(
    bool IncludeInactive = false)
    : IQuery<IReadOnlyList<ShippingListItemDto>>, ICacheableQuery
{
    public string CacheKey => $"shippings:list:inactive={IncludeInactive}";

    public TimeSpan? Expiry => TimeSpan.FromMinutes(30);
}