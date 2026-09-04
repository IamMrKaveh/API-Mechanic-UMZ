using Application.Inventory.Features.Shared;

namespace Application.Inventory.Features.Queries.GetAllWarehouses;

public record GetAllWarehousesQuery : IQuery<IReadOnlyList<WarehouseDto>>, ICacheableQuery
{
    public string CacheKey => "warehouses:all";

    public TimeSpan? Expiry => TimeSpan.FromHours(1);
}