using Application.Cache.Contracts;
using Application.Category.Features.Queries.GetCategoryTree;

namespace Tests.Application.Category.Features.Queries.GetCategoryTree;

public class GetCategoryTreeQueryCacheTests
{
    [Fact]
    public void CacheKey_IsFixedTreeKey()
    {
        var query = new GetCategoryTreeQuery();

        ((ICacheableQuery)query).CacheKey.ShouldBe("categories:tree");
    }

    [Fact]
    public void Expiry_IsOneHour()
    {
        var query = new GetCategoryTreeQuery();

        ((ICacheableQuery)query).Expiry.ShouldBe(TimeSpan.FromHours(1));
    }
}
