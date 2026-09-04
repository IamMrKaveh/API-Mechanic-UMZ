using Application.Cache.Contracts;
using Application.Inventory.Features.Queries.GetAllWarehouses;

namespace Tests.Application.Inventory.Features.Queries.GetAllWarehouses;

public class GetAllWarehousesQueryCacheTests
{
    [Fact]
    public void CacheKey_IsFixedAllKey()
    {
        var query = new GetAllWarehousesQuery();

        ((ICacheableQuery)query).CacheKey.ShouldBe("warehouses:all");
    }

    [Fact]
    public void Expiry_IsOneHour()
    {
        var query = new GetAllWarehousesQuery();

        ((ICacheableQuery)query).Expiry.ShouldBe(TimeSpan.FromHours(1));
    }
}
