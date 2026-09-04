using Application.Cache.Contracts;
using Application.Location.Features.Queries.GetStates;

namespace Tests.Application.Location.Features.Queries.GetStates;

public class GetStatesQueryCacheTests
{
    [Fact]
    public void CacheKey_ContainsPageAndPageSize()
    {
        var query = new GetStatesQuery(Page: 2, PageSize: 25);

        ((ICacheableQuery)query).CacheKey.ShouldBe("location:states:page=2:size=25");
    }

    [Fact]
    public void Expiry_IsTwentyFourHours()
    {
        var query = new GetStatesQuery();

        ((ICacheableQuery)query).Expiry.ShouldBe(TimeSpan.FromHours(24));
    }
}
